using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.SetupTwoFactor;

/// <summary>
/// Handler for setting up TOTP-based two-factor authentication.
/// </summary>
public sealed class SetupTwoFactorCommandHandler : ICommandHandler<SetupTwoFactorCommand, Result<TwoFactorSetupDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorAuthenticator _twoFactorAuthenticator;

    public SetupTwoFactorCommandHandler(
        IUserRepository userRepository,
        ITwoFactorAuthenticator twoFactorAuthenticator)
    {
        _userRepository = userRepository;
        _twoFactorAuthenticator = twoFactorAuthenticator;
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

        // Generate the QR code URI
        var qrCodeUri = _twoFactorAuthenticator.GenerateQrCodeUri(user.Email.Value, secret);

        // Return the setup information (not yet persisted - user must confirm with a code)
        return new TwoFactorSetupDto(secret, qrCodeUri);
    }
}
