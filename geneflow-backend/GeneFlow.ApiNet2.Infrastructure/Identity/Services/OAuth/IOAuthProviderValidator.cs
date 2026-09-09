using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Services.OAuth;

/// <summary>
/// Internal interface for provider-specific OAuth validation.
/// </summary>
internal interface IOAuthProviderValidator
{
    /// <summary>
    /// Validates an OAuth token for this specific provider.
    /// </summary>
    /// <param name="token">The access token or ID token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the validated OAuth user info.</returns>
    Task<Result<OAuthUserInfo>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
