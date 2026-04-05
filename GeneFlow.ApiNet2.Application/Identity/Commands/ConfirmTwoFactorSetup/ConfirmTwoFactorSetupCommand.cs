using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.ConfirmTwoFactorSetup;

/// <summary>
/// Command to confirm TOTP-based two-factor authentication setup.
/// Validates the code from the authenticator app and persists the secret.
/// </summary>
/// <param name="UserId">The user's ID.</param>
/// <param name="Secret">The Base32-encoded secret from setup.</param>
/// <param name="Code">The 6-digit code from the authenticator app.</param>
public sealed record ConfirmTwoFactorSetupCommand(
    string UserId,
    string Secret,
    string Code) : ICommand<Result>;
