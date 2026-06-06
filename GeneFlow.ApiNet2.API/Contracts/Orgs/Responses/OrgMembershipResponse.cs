namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Responses;

/// <summary>
/// Lightweight projection of an org along with the current user's role
/// inside it. Returned by <c>GET /api/v1/me/orgs</c>.
/// </summary>
public sealed record OrgMembershipResponse(
    string OrgId,
    string Handle,
    string Name,
    string? AvatarUrl,
    string Role);
