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
        // Parse the provider
        var provider = ExternalProvider.FromName(request.Provider);
        if (provider is null)
            return Result.Failure<LoginResultDto>(OAuthErrors.ProviderNotSupported(request.Provider));

        // Validate the token with the provider
        var validationResult = await _oAuthValidator.ValidateTokenAsync(
            provider, request.Token, cancellationToken);

        if (validationResult.IsFailure)
            return Result.Failure<LoginResultDto>(validationResult.Error);

        var oAuthUserInfo = validationResult.Value;

        // Check if user already exists with this external login
        var existingUser = await _userRepository.GetByExternalLoginAsync(
            provider, oAuthUserInfo.ProviderKey, cancellationToken);

        if (existingUser is not null)
        {
            return await LoginExistingUser(existingUser, cancellationToken);
        }

        // Check if user exists with this email
        var emailResult = Email.Create(oAuthUserInfo.Email);
        if (emailResult.IsFailure)
            return Result.Failure<LoginResultDto>(emailResult.Error);

        var userByEmail = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        _logger.LogInformation("OAuthLogin: GetByEmailAsync returned {Result}", userByEmail != null ? "user found" : "null");

        if (userByEmail is not null)
        {
            // Link the OAuth provider to existing account
            var linkResult = userByEmail.LinkExternalLogin(
                provider,
                oAuthUserInfo.ProviderKey,
                oAuthUserInfo.DisplayName);

            if (linkResult.IsFailure)
                return Result.Failure<LoginResultDto>(linkResult.Error);

            return await LoginExistingUser(userByEmail, cancellationToken);
        }

        // Create a new user
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

        // Generate username from email or display name
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

        // Generate user ID
        _logger.LogInformation("CreateNewOAuthUser: Generating sequence ID from Redis...");
        var sequenceId = await _sequenceGenerator.NextAsync("user", cancellationToken);
        _logger.LogInformation("CreateNewOAuthUser: Sequence ID generated: {SequenceId}", sequenceId);

        var userId = UserId.FromSequence(sequenceId);
        _logger.LogInformation("CreateNewOAuthUser: UserId created: {UserId}", userId.Value);

        // Create user via OAuth
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

    private static string SanitizeUsername(string input)
    {
        // Remove non-ASCII alphanumeric characters and limit length
        // Only allow a-z, A-Z, 0-9, and underscore
        var sanitized = new string(input
            .Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
            .ToArray());

        return sanitized.Length >= 3 ? sanitized[..Math.Min(sanitized.Length, 20)] : "user";
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

            if (counter > 100)
                username = $"{baseUsername}{Guid.NewGuid():N}"[..20];
        }
    }
}
