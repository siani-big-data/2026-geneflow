using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.UnlinkExternalLogin;

/// <summary>
/// Command to unlink an external OAuth login from the current user.
/// </summary>
/// <param name="UserId">The current user's ID.</param>
/// <param name="Provider">The OAuth provider name to unlink.</param>
public sealed record UnlinkExternalLoginCommand(
    string UserId,
    string Provider) : ICommand<Result>;
