namespace GeneFlow.ApiNet2.API.Contracts.Identity.Responses;

/// <summary>
/// Response model for user information.
/// </summary>
public sealed record UserResponse(
    string Id,
    string Email,
    string Username,
    bool IsActive,
    bool EmailVerified,
    bool TwoFactorEnabled,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? ModifiedAt);
