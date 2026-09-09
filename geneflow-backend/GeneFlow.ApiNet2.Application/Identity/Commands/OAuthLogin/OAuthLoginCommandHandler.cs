using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;
using RefreshTokenVO = GeneFlow.ApiNet2.Domain.Identity.ValueObjects.RefreshToken;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.OAuthLogin;

/// <summary>
/// Handler for OAuth login command.
/// </summary>
public sealed class OAuthLoginCommandHandler
    : ICommandHandler<OAuthLoginCommand, Result<LoginResultDto>>
{
    /// <summary>Minimum length for an auto-generated username.</summary>
    private const int MinUsernameLength = 3;

    /// <summary>Maximum length for an auto-generated username.</summary>
    private const int MaxUsernameLength = 20;

    /// <summary>Fallback username used when sanitization yields too few characters.</summary>
    private const string FallbackUsername = "user";

    /// <summary>
    /// Maximum number of suffix attempts before falling back to a GUID-based
    /// username when generating a unique handle.
    /// </summary>
    private const int MaxUsernameSuffixAttempts = 100;

    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IOAuthTokenValidator _oAuthValidator;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly ILogger<OAuthLoginCommandHandler> _logger;

    public OAuthLoginCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IOAuthTokenValidator oAuthValidator,
        IJwtTokenGenerator tokenGenerator,
        ISequenceGenerator sequenceGenerator,
        ILogger<OAuthLoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _oAuthValidator = oAuthValidator;
        _tokenGenerator = tokenGenerator;
        _sequenceGenerator = sequenceGenerator;
        _logger = logger;
    }

    public async Task<Result<LoginResultDto>> Handle(
        OAuthLoginCommand request,
        CancellationToken cancellationToken)
    {
        var provider = ExternalProvider.FromName(request.Provider);
        if (provider is null)
            return Result.Failure<LoginResultDto>(OAuthErrors.ProviderNotSupported(request.Provider));

        var validationResult = await _oAuthValidator.ValidateTokenAsync(
            provider, request.Token, cancellationToken);

        if (validationResult.IsFailure)
            return Result.Failure<LoginResultDto>(validationResult.Error);

        var oAuthUserInfo = validationResult.Value;

        var existingUser = await _userRepository.GetByExternalLoginAsync(
            provider, oAuthUserInfo.ProviderKey, cancellationToken);

        if (existingUser is not null)
        {
            return await LoginExistingUser(existingUser, cancellationToken);
        }

        var emailResult = Email.Create(oAuthUserInfo.Email);
        if (emailResult.IsFailure)
            return Result.Failure<LoginResultDto>(emailResult.Error);

        var userByEmail = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        _logger.LogInformation("OAuthLogin: GetByEmailAsync returned {Result}", userByEmail != null ? "user found" : "null");

        if (userByEmail is not null)
        {
            var linkResult = userByEmail.LinkExternalLogin(
                provider,
                oAuthUserInfo.ProviderKey,
                oAuthUserInfo.DisplayName);

            if (linkResult.IsFailure)
                return Result.Failure<LoginResultDto>(linkResult.Error);

            return await LoginExistingUser(userByEmail, cancellationToken);
        }

        _logger.LogInformation("OAuthLogin: Creating new OAuth user for {Email}", oAuthUserInfo.Email);
        return await CreateNewOAuthUser(
            provider,
            oAuthUserInfo,
            emailResult.Value,
            cancellationToken);
    }

    private async Task<Result<LoginResultDto>> LoginExistingUser(
        User user,
        CancellationToken cancellationToken)
    {
        if (!user.IsActive)
            return Result.Failure<LoginResultDto>(UserErrors.UserDeactivated);

        if (user.IsDeleted)
            return Result.Failure<LoginResultDto>(UserErrors.UserNotFound);

        user.RecordSuccessfulLogin();

        var (accessToken, accessTokenExpiry) = _tokenGenerator.GenerateAccessToken(user);
        var (refreshTokenValue, refreshTokenExpiry) = _tokenGenerator.GenerateRefreshToken();

        var refreshTokenResult = RefreshTokenVO.Create(refreshTokenValue, refreshTokenExpiry);
        if (refreshTokenResult.IsFailure)
            return Result.Failure<LoginResultDto>(refreshTokenResult.Error);

        user.AddRefreshToken(refreshTokenResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResultDto
        {
            User = user.ToDto(),
            Tokens = new AuthTokensDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = accessTokenExpiry
            },
            RequiresTwoFactor = false
        };
    }

    private async Task<Result<LoginResultDto>> CreateNewOAuthUser(
        ExternalProvider provider,
        OAuthUserInfo oAuthUserInfo,
        Email email,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateNewOAuthUser: Starting for email {Email}", email.Value);

        var baseUsername = !string.IsNullOrWhiteSpace(oAuthUserInfo.DisplayName)
            ? SanitizeUsername(oAuthUserInfo.DisplayName)
            : SanitizeUsername(email.Value.Split('@')[0]);
        _logger.LogInformation("CreateNewOAuthUser: Base username is {Username}", baseUsername);

        _logger.LogInformation("CreateNewOAuthUser: Generating unique username...");
        var username = await GenerateUniqueUsernameAsync(baseUsername, cancellationToken);
        _logger.LogInformation("CreateNewOAuthUser: Unique username generated: {Username}", username);

        var usernameResult = Username.Create(username);
        if (usernameResult.IsFailure)
            return Result.Failure<LoginResultDto>(usernameResult.Error);

        _logger.LogInformation("CreateNewOAuthUser: Generating sequence ID from Redis...");
        var sequenceId = await _sequenceGenerator.NextAsync("user", cancellationToken);
        _logger.LogInformation("CreateNewOAuthUser: Sequence ID generated: {SequenceId}", sequenceId);

        var userId = UserId.FromSequence(sequenceId);
        _logger.LogInformation("CreateNewOAuthUser: UserId created: {UserId}", userId.Value);

        _logger.LogInformation("CreateNewOAuthUser: Creating User entity...");
        var userResult = User.CreateFromOAuth(
            userId,
            email,
            usernameResult.Value,
            provider,
            oAuthUserInfo.ProviderKey,
            oAuthUserInfo.DisplayName);

        if (userResult.IsFailure)
        {
            _logger.LogError("CreateNewOAuthUser: User.CreateFromOAuth failed: {Error}", userResult.Error.Code);
            return Result.Failure<LoginResultDto>(userResult.Error);
        }

        var user = userResult.Value;
        _logger.LogInformation("CreateNewOAuthUser: User entity created, adding to repository...");

        await _userRepository.AddAsync(user, cancellationToken);
        _logger.LogInformation("CreateNewOAuthUser: User added to repository");

        var (accessToken, accessTokenExpiry) = _tokenGenerator.GenerateAccessToken(user);
        var (refreshTokenValue, refreshTokenExpiry) = _tokenGenerator.GenerateRefreshToken();
        _logger.LogInformation("CreateNewOAuthUser: Tokens generated");

        var refreshTokenResult = RefreshTokenVO.Create(refreshTokenValue, refreshTokenExpiry);
        if (refreshTokenResult.IsFailure)
            return Result.Failure<LoginResultDto>(refreshTokenResult.Error);

        user.AddRefreshToken(refreshTokenResult.Value);
        _logger.LogInformation("CreateNewOAuthUser: Saving changes...");
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CreateNewOAuthUser: Changes saved successfully");

        return new LoginResultDto
        {
            User = user.ToDto(),
            Tokens = new AuthTokensDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = accessTokenExpiry
            },
            RequiresTwoFactor = false
        };
    }

    /// <summary>
    /// Removes non-ASCII alphanumeric characters (keeps a-z, A-Z, 0-9, _),
    /// then truncates to <see cref="MaxUsernameLength"/>. Falls back to
    /// <see cref="FallbackUsername"/> if the result has fewer than
    /// <see cref="MinUsernameLength"/> characters.
    /// </summary>
    private static string SanitizeUsername(string input)
    {
        var sanitized = new string(input
            .Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
            .ToArray());

        return sanitized.Length >= MinUsernameLength
            ? sanitized[..Math.Min(sanitized.Length, MaxUsernameLength)]
            : FallbackUsername;
    }

    private async Task<string> GenerateUniqueUsernameAsync(string baseUsername, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GenerateUniqueUsername: Starting with base {Base}", baseUsername);
        var username = baseUsername;
        var counter = 1;

        while (true)
        {
            _logger.LogInformation("GenerateUniqueUsername: Trying username {Username}", username);
            var usernameResult = Username.Create(username);
            if (usernameResult.IsSuccess)
            {
                _logger.LogInformation("GenerateUniqueUsername: Checking if exists...");
                var exists = await _userRepository.ExistsWithUsernameAsync(usernameResult.Value, cancellationToken);
                _logger.LogInformation("GenerateUniqueUsername: Exists = {Exists}", exists);
                if (!exists)
                    return username;
            }
            else
            {
                _logger.LogInformation("GenerateUniqueUsername: Username.Create failed for {Username}", username);
            }

            username = $"{baseUsername}{counter}";
            counter++;

            if (counter > MaxUsernameSuffixAttempts)
                username = $"{baseUsername}{Guid.NewGuid():N}"[..MaxUsernameLength];
        }
    }
}
