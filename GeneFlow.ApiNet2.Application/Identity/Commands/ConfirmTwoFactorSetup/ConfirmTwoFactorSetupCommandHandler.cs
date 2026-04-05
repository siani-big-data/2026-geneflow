using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.ConfirmTwoFactorSetup;

/// <summary>
/// Handler for confirming TOTP-based two-factor authentication setup.
/// </summary>
public sealed class ConfirmTwoFactorSetupCommandHandler : ICommandHandler<ConfirmTwoFactorSetupCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly ITwoFactorAuthenticator _twoFactorAuthenticator;

    public ConfirmTwoFactorSetupCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        ITwoFactorAuthenticator twoFactorAuthenticator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _twoFactorAuthenticator = twoFactorAuthenticator;
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

        // Validate the code against the provided secret
        if (!_twoFactorAuthenticator.ValidateCode(request.Secret, request.Code))
            return Result.Failure(UserErrors.InvalidTwoFactorCode);

        // Encrypt the secret for storage
        var encryptedSecret = _twoFactorAuthenticator.EncryptSecret(request.Secret);

        // Enable TOTP on the user
        var result = user.EnableTotpTwoFactor(encryptedSecret);
        if (result.IsFailure)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
