using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
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

    public OAuthLoginCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IOAuthTokenValidator oAuthValidator,
        IJwtTokenGenerator tokenGenerator,
        ISequenceGenerator sequenceGenerator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _oAuthValidator = oAuthValidator;
        _tokenGenerator = tokenGenerator;
        _sequenceGenerator = sequenceGenerator;
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
        // Generate username from email or display name
        var baseUsername = !string.IsNullOrWhiteSpace(oAuthUserInfo.DisplayName)
            ? SanitizeUsername(oAuthUserInfo.DisplayName)
            : SanitizeUsername(email.Value.Split('@')[0]);

        var username = await GenerateUniqueUsernameAsync(baseUsername, cancellationToken);
        var usernameResult = Username.Create(username);
        if (usernameResult.IsFailure)
            return Result.Failure<LoginResultDto>(usernameResult.Error);

        // Generate user ID
        var sequenceId = await _sequenceGenerator.NextAsync("user", cancellationToken);
        var userId = UserId.FromSequence(sequenceId);

        // Create user via OAuth
        var userResult = User.CreateFromOAuth(
            userId,
            email,
            usernameResult.Value,
            provider,
            oAuthUserInfo.ProviderKey,
            oAuthUserInfo.DisplayName);

        if (userResult.IsFailure)
            return Result.Failure<LoginResultDto>(userResult.Error);

        var user = userResult.Value;

        await _userRepository.AddAsync(user, cancellationToken);

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

    private static string SanitizeUsername(string input)
    {
        // Remove non-alphanumeric characters and limit length
        var sanitized = new string(input
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .ToArray());

        return sanitized.Length >= 3 ? sanitized[..Math.Min(sanitized.Length, 20)] : "user";
    }

    private async Task<string> GenerateUniqueUsernameAsync(string baseUsername, CancellationToken cancellationToken)
    {
        var username = baseUsername;
        var counter = 1;

        while (true)
        {
            var usernameResult = Username.Create(username);
            if (usernameResult.IsSuccess)
            {
                var exists = await _userRepository.ExistsWithUsernameAsync(usernameResult.Value, cancellationToken);
                if (!exists)
                    return username;
            }

            username = $"{baseUsername}{counter}";
            counter++;

            if (counter > 100)
                username = $"{baseUsername}{Guid.NewGuid():N}"[..20];
        }
    }
}
