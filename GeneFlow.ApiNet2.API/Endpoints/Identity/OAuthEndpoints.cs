using System.Net.Http.Headers;
using System.Text.Json;
using GeneFlow.ApiNet2.API.Contracts.Identity.Requests;
using GeneFlow.ApiNet2.API.Contracts.Identity.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Routes;
using GeneFlow.ApiNet2.Application.Identity.Commands.OAuthLogin;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GeneFlow.ApiNet2.API.Endpoints.Identity;

/// <summary>
/// OAuth authentication endpoints (login via Google/GitHub and GitHub code exchange).
/// </summary>
public sealed class OAuthEndpoints : IEndpoint
{
    private static readonly JsonSerializerOptions GitHubResponseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Auth.OAuth)
            .WithTags("OAuth")
            .WithOpenApi();

        group.MapPost("/{provider}", OAuthLogin)
            .WithName("OAuth_Login")
            .WithSummary("Login or register via OAuth provider")
            .WithDescription("Authenticates a user using an OAuth provider (Google, GitHub). " +
                             "If the user doesn't exist, a new account is created.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status401Unauthorized);

        group.MapPost("/github/exchange", ExchangeGitHubCode)
            .WithName("OAuth_ExchangeGitHubCode")
            .WithSummary("Exchange GitHub authorization code for access token")
            .WithDescription("Exchanges a GitHub OAuth authorization code for an access token.")
            .Produces<GitHubTokenResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> OAuthLogin(
        [FromRoute] string provider,
        [FromBody] OAuthLoginRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new OAuthLoginCommand(provider, request.Token);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        var loginResult = result.Value;
        return Results.Ok(new LoginResponse(
            loginResult.User.ToResponse(),
            loginResult.Tokens?.ToResponse(),
            loginResult.RequiresTwoFactor));
    }

    private static async Task<IResult> ExchangeGitHubCode(
        [FromBody] GitHubCodeExchangeRequest request,
        [FromServices] IConfiguration configuration,
        [FromServices] IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var clientId = configuration["OAuth:GitHub:ClientId"];
        var clientSecret = configuration["OAuth:GitHub:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return Results.BadRequest(new ApiError("OAuth.NotConfigured", "GitHub OAuth is not configured."));
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = request.Code,
                }),
            };

            using var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Results.BadRequest(new ApiError("OAuth.ExchangeFailed", "Failed to exchange code with GitHub."));
            }

            var tokenResponse = JsonSerializer.Deserialize<GitHubOAuthResponse>(content, GitHubResponseSerializerOptions);

            if (tokenResponse?.AccessToken is null)
            {
                if (content.Contains("error", StringComparison.Ordinal))
                {
                    return Results.BadRequest(new ApiError("OAuth.InvalidCode", "The authorization code is invalid or expired."));
                }
                return Results.BadRequest(new ApiError("OAuth.NoToken", "No access token received from GitHub."));
            }

            return Results.Ok(new GitHubTokenResponse(tokenResponse.AccessToken));
        }
        catch (HttpRequestException)
        {
            return Results.BadRequest(new ApiError("OAuth.Error", "Network error while exchanging the code."));
        }
        catch (TaskCanceledException)
        {
            return Results.BadRequest(new ApiError("OAuth.Error", "Timeout while exchanging the code."));
        }
        catch (JsonException)
        {
            return Results.BadRequest(new ApiError("OAuth.Error", "Invalid response received from GitHub."));
        }
    }
}
