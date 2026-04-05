using GeneFlow.ApiNet2.Application.Identity.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.OAuthLogin;

/// <summary>
/// Command to login or register via OAuth provider.
/// </summary>
/// <param name="Provider">The OAuth provider name (Google, GitHub).</param>
/// <param name="Token">The access token or ID token from the provider.</param>
public sealed record OAuthLoginCommand(
    string Provider,
    string Token) : ICommand<Result<LoginResultDto>>;
