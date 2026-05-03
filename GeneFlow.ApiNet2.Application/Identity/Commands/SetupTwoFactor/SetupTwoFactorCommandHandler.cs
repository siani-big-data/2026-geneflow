using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.SetupTwoFactor;

/// <summary>
/// Handler for setting up TOTP-based two-factor authentication.
/// </summary>
public sealed class SetupTwoFactorCommandHandler : ICommandHandler<SetupTwoFactorCommand, Result<TwoFactorSetupDto>>
{
    private const string CacheKeyPrefix = "2fa-setup:";
    private static readonly TimeSpan SecretCacheExpiration = TimeSpan.FromMinutes(10);

    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorAuthenticator _twoFactorAuthenticator;
    private readonly ICacheService _cacheService;

    public SetupTwoFactorCommandHandler(
        IUserRepository userRepository,
        ITwoFactorAuthenticator twoFactorAuthenticator,
        ICacheService cacheService)
    {
        _userRepository = userRepository;
        _twoFactorAuthenticator = twoFactorAuthenticator;
        _cacheService = cacheService;
    }

    public async Task<Result<TwoFactorSetupDto>> Handle(SetupTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<TwoFactorSetupDto>(UserErrors.UserNotFound);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure<TwoFactorSetupDto>(UserErrors.UserNotFound);

        if (user.TwoFactorEnabled && user.IsTotpConfigured)
            return Result.Failure<TwoFactorSetupDto>(UserErrors.TwoFactorAlreadyEnabled);

        // Generate a new secret
        var secret = _twoFactorAuthenticator.GenerateSecret();

        // Store the secret in cache (server-side) for confirmation step
        var cacheKey = $"{CacheKeyPrefix}{request.UserId}";
        await _cacheService.SetAsync(cacheKey, secret, SecretCacheExpiration, cancellationToken);

        // Generate the QR code URI
        var qrCodeUri = _twoFactorAuthenticator.GenerateQrCodeUri(user.Email.Value, secret);

        // Return the setup information (secret shown for manual entry, but not sent back for confirmation)
        return new TwoFactorSetupDto(secret, qrCodeUri);
    }
}
