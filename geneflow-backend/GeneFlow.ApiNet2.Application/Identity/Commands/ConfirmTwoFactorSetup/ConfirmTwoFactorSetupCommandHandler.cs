using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.ConfirmTwoFactorSetup;

/// <summary>
/// Handler for confirming TOTP-based two-factor authentication setup.
/// Retrieves the secret from server-side cache for security.
/// </summary>
public sealed class ConfirmTwoFactorSetupCommandHandler : ICommandHandler<ConfirmTwoFactorSetupCommand, Result>
{
    private const string CacheKeyPrefix = "2fa-setup:";

    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly ITwoFactorAuthenticator _twoFactorAuthenticator;
    private readonly ICacheService _cacheService;

    public ConfirmTwoFactorSetupCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        ITwoFactorAuthenticator twoFactorAuthenticator,
        ICacheService cacheService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _twoFactorAuthenticator = twoFactorAuthenticator;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(ConfirmTwoFactorSetupCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(UserErrors.UserNotFound);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        if (user.TwoFactorEnabled && user.IsTotpConfigured)
            return Result.Failure(UserErrors.TwoFactorAlreadyEnabled);

        // Retrieve the secret from server-side cache
        var cacheKey = $"{CacheKeyPrefix}{request.UserId}";
        var secret = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(secret))
            return Result.Failure(UserErrors.TwoFactorSetupExpired);

        // Validate the code against the cached secret
        if (!_twoFactorAuthenticator.ValidateCode(secret, request.Code))
            return Result.Failure(UserErrors.InvalidTwoFactorCode);

        // Encrypt the secret for storage
        var encryptedSecret = _twoFactorAuthenticator.EncryptSecret(secret);

        // Enable TOTP on the user
        var result = user.EnableTotpTwoFactor(encryptedSecret);
        if (result.IsFailure)
            return result;

        // Remove the secret from cache
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
