namespace GeneFlow.ApiNet2.API.Contracts.Identity.Requests;

/// <summary>
/// Request payload to exchange a GitHub OAuth authorization code for an access token.
/// </summary>
/// <param name="Code">Authorization code returned by GitHub after user consent.</param>
public sealed record GitHubCodeExchangeRequest(string Code);
