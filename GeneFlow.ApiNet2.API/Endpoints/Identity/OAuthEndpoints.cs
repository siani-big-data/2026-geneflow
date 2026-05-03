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
/// OAuth authentication endpoints.
/// </summary>
public sealed class OAuthEndpoints : IEndpoint
{
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
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = request.Code
                })
            };

            var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Results.BadRequest(new ApiError("OAuth.ExchangeFailed", "Failed to exchange code with GitHub."));
            }

            var tokenResponse = JsonSerializer.Deserialize<GitHubOAuthResponse>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (tokenResponse?.AccessToken == null)
            {
                // GitHub returns error in the same format when code is invalid
                if (content.Contains("error"))
                {
                    return Results.BadRequest(new ApiError("OAuth.InvalidCode", "The authorization code is invalid or expired."));
                }
                return Results.BadRequest(new ApiError("OAuth.NoToken", "No access token received from GitHub."));
            }

            return Results.Ok(new GitHubTokenResponse(tokenResponse.AccessToken));
        }
        catch (Exception)
        {
            return Results.BadRequest(new ApiError("OAuth.Error", "An error occurred while exchanging the code."));
        }
    }
}

/// <summary>
/// Request to exchange GitHub authorization code.
/// </summary>
public sealed record GitHubCodeExchangeRequest(string Code);

/// <summary>
/// Response with GitHub access token.
/// </summary>
public sealed record GitHubTokenResponse(string AccessToken);

/// <summary>
/// GitHub OAuth token response (internal).
/// </summary>
internal sealed record GitHubOAuthResponse(
    string? AccessToken,
    string? TokenType,
    string? Scope,
    string? Error,
    string? ErrorDescription);
