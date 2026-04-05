using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.SetupTwoFactor;

/// <summary>
/// Command to setup TOTP-based two-factor authentication.
/// Returns the secret and QR code URI for the user to scan.
/// </summary>
public sealed record SetupTwoFactorCommand(string UserId) : ICommand<Result<TwoFactorSetupDto>>;
