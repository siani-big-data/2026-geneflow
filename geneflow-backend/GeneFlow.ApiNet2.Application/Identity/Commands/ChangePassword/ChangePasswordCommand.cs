using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.ChangePassword;

/// <summary>
/// Command to change an authenticated user's password.
/// </summary>
public sealed record ChangePasswordCommand(
    UserId UserId,
    string CurrentPassword,
    string NewPassword) : ICommand<Result>;
