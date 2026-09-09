using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Services.OAuth;

/// <summary>
/// Orchestrates OAuth token validation across different providers.
/// </summary>
internal sealed class OAuthTokenValidator : IOAuthTokenValidator
{
    private readonly GoogleTokenValidator _googleValidator;
    private readonly GitHubTokenValidator _gitHubValidator;

    public OAuthTokenValidator(
        GoogleTokenValidator googleValidator,
        GitHubTokenValidator gitHubValidator)
    {
        _googleValidator = googleValidator;
        _gitHubValidator = gitHubValidator;
    }

    public Task<Result<OAuthUserInfo>> ValidateTokenAsync(
        ExternalProvider provider,
        string token,
        CancellationToken cancellationToken = default)
    {
        IOAuthProviderValidator validator = provider.Name switch
        {
            nameof(ExternalProvider.Google) => _googleValidator,
            nameof(ExternalProvider.GitHub) => _gitHubValidator,
            _ => throw new NotSupportedException($"Provider {provider.Name} is not supported")
        };

        return validator.ValidateTokenAsync(token, cancellationToken);
    }
}
