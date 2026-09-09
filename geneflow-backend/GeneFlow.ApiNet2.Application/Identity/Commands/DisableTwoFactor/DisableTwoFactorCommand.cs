using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.DisableTwoFactor;

/// <summary>
/// Command to disable two-factor authentication.
/// </summary>
public sealed record DisableTwoFactorCommand(string UserId) : ICommand<Result>;
