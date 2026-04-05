using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.LinkExternalLogin;

/// <summary>
/// Command to link an external OAuth login to the current user.
/// </summary>
/// <param name="UserId">The current user's ID.</param>
/// <param name="Provider">The OAuth provider name (Google, GitHub).</param>
/// <param name="Token">The access token or ID token from the provider.</param>
public sealed record LinkExternalLoginCommand(
    string UserId,
    string Provider,
    string Token) : ICommand<Result>;
