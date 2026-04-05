using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Services.OAuth;

/// <summary>
/// Validates Google OAuth tokens.
/// </summary>
internal sealed class GoogleTokenValidator : IOAuthProviderValidator
{
    private const string TokenInfoUrl = "https://oauth2.googleapis.com/tokeninfo?id_token=";
    private const string UserInfoUrl = "https://www.googleapis.com/oauth2/v3/userinfo";

    private readonly HttpClient _httpClient;
    private readonly OAuthSettings _settings;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(
        HttpClient httpClient,
        IOptions<OAuthSettings> settings,
        ILogger<GoogleTokenValidator> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<OAuthUserInfo>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to validate as ID token first
            var tokenInfoResponse = await _httpClient.GetAsync(
                $"{TokenInfoUrl}{Uri.EscapeDataString(token)}",
                cancellationToken);

            if (tokenInfoResponse.IsSuccessStatusCode)
            {
                var tokenInfo = await tokenInfoResponse.Content.ReadFromJsonAsync<GoogleTokenInfo>(cancellationToken);

                if (tokenInfo is null)
                    return Result.Failure<OAuthUserInfo>(OAuthErrors.InvalidToken);

                // Verify the audience matches our client ID
                if (!string.IsNullOrEmpty(_settings.Google.ClientId) &&
                    tokenInfo.Aud != _settings.Google.ClientId)
                {
                    _logger.LogWarning("Google token audience mismatch. Expected: {Expected}, Got: {Got}",
                        _settings.Google.ClientId, tokenInfo.Aud);
                    return Result.Failure<OAuthUserInfo>(OAuthErrors.InvalidToken);
                }

                return new OAuthUserInfo
                {
                    ProviderKey = tokenInfo.Sub,
                    Email = tokenInfo.Email,
                    DisplayName = tokenInfo.Name,
                    AvatarUrl = tokenInfo.Picture
                };
            }

            // Fall back to treating it as an access token
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoUrl);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userInfoResponse = await _httpClient.SendAsync(request, cancellationToken);

            if (!userInfoResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token validation failed with status: {Status}",
                    userInfoResponse.StatusCode);
                return Result.Failure<OAuthUserInfo>(OAuthErrors.InvalidToken);
            }

            var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken);

            if (userInfo is null || string.IsNullOrEmpty(userInfo.Sub) || string.IsNullOrEmpty(userInfo.Email))
                return Result.Failure<OAuthUserInfo>(OAuthErrors.InvalidToken);

            return new OAuthUserInfo
            {
                ProviderKey = userInfo.Sub,
                Email = userInfo.Email,
                DisplayName = userInfo.Name,
                AvatarUrl = userInfo.Picture
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return Result.Failure<OAuthUserInfo>(OAuthErrors.ValidationFailed(ex.Message));
        }
    }

    private sealed class GoogleTokenInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        [JsonPropertyName("aud")]
        public string Aud { get; set; } = string.Empty;
    }

    private sealed class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }
}
