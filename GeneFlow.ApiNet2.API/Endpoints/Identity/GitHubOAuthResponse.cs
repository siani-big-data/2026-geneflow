namespace GeneFlow.ApiNet2.API.Endpoints.Identity;

/// <summary>
/// Internal representation of GitHub's token endpoint response payload.
/// </summary>
/// <param name="AccessToken">Access token returned on success.</param>
/// <param name="TokenType">Token type (typically <c>bearer</c>).</param>
/// <param name="Scope">Granted OAuth scopes.</param>
/// <param name="Error">Error code returned by GitHub on failure.</param>
/// <param name="ErrorDescription">Human readable error description returned by GitHub on failure.</param>
internal sealed record GitHubOAuthResponse(
    string? AccessToken,
    string? TokenType,
    string? Scope,
    string? Error,
    string? ErrorDescription);
