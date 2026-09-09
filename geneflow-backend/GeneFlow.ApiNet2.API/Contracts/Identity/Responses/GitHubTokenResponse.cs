namespace GeneFlow.ApiNet2.API.Contracts.Identity.Responses;

/// <summary>
/// Response containing the GitHub access token obtained from a successful code exchange.
/// </summary>
/// <param name="AccessToken">GitHub OAuth access token usable to retrieve user information.</param>
public sealed record GitHubTokenResponse(string AccessToken);
