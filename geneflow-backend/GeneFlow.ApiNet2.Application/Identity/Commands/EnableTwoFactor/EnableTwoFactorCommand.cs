using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.EnableTwoFactor;

/// <summary>
/// Command to enable two-factor authentication.
/// </summary>
public sealed record EnableTwoFactorCommand(string UserId) : ICommand<Result>;
