using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.ResetPassword;

/// <summary>
/// Command to reset a user's password.
/// </summary>
public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword) : ICommand<Result>;
