using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using RefreshTokenVO = GeneFlow.ApiNet2.Domain.Identity.ValueObjects.RefreshToken;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.Login;

/// <summary>
/// Handler for user login command.
/// </summary>
public sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, Result<LoginResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IUserAuthenticationValidator _authValidator;
    private readonly ITwoFactorAuthenticator _twoFactorAuthenticator;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public LoginCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IUserAuthenticationValidator authValidator,
        ITwoFactorAuthenticator twoFactorAuthenticator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _authValidator = authValidator;
        _twoFactorAuthenticator = twoFactorAuthenticator;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResultDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailOrUsernameAsync(
            request.EmailOrUsername, cancellationToken);

        if (user is null)
            return Result.Failure<LoginResultDto>(UserErrors.InvalidCredentials);

        var canAuthResult = _authValidator.ValidateCanAuthenticate(user);
        if (canAuthResult.IsFailure)
            return Result.Failure<LoginResultDto>(canAuthResult.Error);

        var passwordResult = _authValidator.ValidatePassword(user, request.Password);
        if (passwordResult.IsFailure)
        {
            user.RecordFailedLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<LoginResultDto>(UserErrors.InvalidCredentials);
        }

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TwoFactorCode))
            {
                return Result.Success(new LoginResultDto
                {
                    User = user.ToDto(),
                    Tokens = null!,
                    RequiresTwoFactor = true
                });
            }

            // Try TOTP validation first if configured
            if (user.IsTotpConfigured)
            {
                var decryptedSecret = _twoFactorAuthenticator.DecryptSecret(user.TotpSecret!);
                if (!_twoFactorAuthenticator.ValidateCode(decryptedSecret, request.TwoFactorCode))
                {
                    return Result.Failure<LoginResultDto>(UserErrors.InvalidTwoFactorCode);
                }
            }
            else
            {
                // Fall back to email-based code validation
                var twoFactorResult = user.ValidateTwoFactorCode(request.TwoFactorCode);
                if (twoFactorResult.IsFailure)
                {
                    return Result.Failure<LoginResultDto>(twoFactorResult.Error);
                }
            }
        }

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
}
