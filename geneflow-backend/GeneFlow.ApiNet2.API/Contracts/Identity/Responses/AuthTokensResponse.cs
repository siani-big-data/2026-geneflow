namespace GeneFlow.ApiNet2.API.Contracts.Identity.Responses;

/// <summary>
/// Response model for authentication tokens.
/// </summary>
public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
