namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Responses;

/// <summary>
/// API response describing an org invitation in any lifecycle state.
/// </summary>
public sealed record OrgInvitationResponse(
    string Id,
    string OrgId,
    string? OrgHandle,
    string? OrgName,
    string InvitedEmail,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt);
