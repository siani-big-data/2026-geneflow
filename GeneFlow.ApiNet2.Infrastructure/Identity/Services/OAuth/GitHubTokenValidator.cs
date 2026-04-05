using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Services.OAuth;

/// <summary>
/// Validates GitHub OAuth tokens.
/// </summary>
internal sealed class GitHubTokenValidator : IOAuthProviderValidator
{
    private const string UserApiUrl = "https://api.github.com/user";
    private const string EmailsApiUrl = "https://api.github.com/user/emails";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubTokenValidator> _logger;

    public GitHubTokenValidator(
        HttpClient httpClient,
        ILogger<GitHubTokenValidator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<OAuthUserInfo>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user info
            using var userRequest = new HttpRequestMessage(HttpMethod.Get, UserApiUrl);
            userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            userRequest.Headers.UserAgent.ParseAdd("GeneFlow-API");

            var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);

            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub user API call failed with status: {Status}",
                    userResponse.StatusCode);
                return Result.Failure<OAuthUserInfo>(OAuthErrors.InvalidToken);
            }

            var userInfo = await userResponse.Content.ReadFromJsonAsync<GitHubUserInfo>(cancellationToken);

            if (userInfo is null || userInfo.Id == 0)
                return Result.Failure<OAuthUserInfo>(OAuthErrors.InvalidToken);

            // If email is not public, fetch from emails endpoint
            var email = userInfo.Email;
            if (string.IsNullOrEmpty(email))
            {
                email = await GetPrimaryEmailAsync(token, cancellationToken);
            }

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("GitHub user {UserId} has no accessible email", userInfo.Id);
                return Result.Failure<OAuthUserInfo>(OAuthErrors.ValidationFailed("No email address available"));
            }

            return new OAuthUserInfo
            {
                ProviderKey = userInfo.Id.ToString(),
                Email = email,
                DisplayName = userInfo.Name ?? userInfo.Login,
                AvatarUrl = userInfo.AvatarUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating GitHub token");
            return Result.Failure<OAuthUserInfo>(OAuthErrors.ValidationFailed(ex.Message));
        }
    }

    private async Task<string?> GetPrimaryEmailAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            using var emailRequest = new HttpRequestMessage(HttpMethod.Get, EmailsApiUrl);
            emailRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            emailRequest.Headers.UserAgent.ParseAdd("GeneFlow-API");

            var emailResponse = await _httpClient.SendAsync(emailRequest, cancellationToken);

            if (!emailResponse.IsSuccessStatusCode)
                return null;

            var emails = await emailResponse.Content.ReadFromJsonAsync<List<GitHubEmail>>(cancellationToken);

            // Return primary verified email, or first verified email
            return emails?
                .Where(e => e.Verified)
                .OrderByDescending(e => e.Primary)
                .Select(e => e.Email)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch GitHub emails");
            return null;
        }
    }

    private sealed class GitHubUserInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    private sealed class GitHubEmail
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }
    }
}
