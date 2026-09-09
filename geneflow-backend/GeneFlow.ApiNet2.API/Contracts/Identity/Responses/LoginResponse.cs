namespace GeneFlow.ApiNet2.API.Contracts.Identity.Responses;

/// <summary>
/// Response model for login result.
/// </summary>
public sealed record LoginResponse(
    UserResponse User,
    AuthTokensResponse? Tokens,
    bool RequiresTwoFactor);
