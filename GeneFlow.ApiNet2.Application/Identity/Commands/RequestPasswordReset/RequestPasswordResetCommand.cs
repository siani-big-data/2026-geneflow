using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.RequestPasswordReset;

/// <summary>
/// Command to request a password reset.
/// </summary>
public sealed record RequestPasswordResetCommand(string Email) : ICommand<Result>;
