namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Responses;

/// <summary>
/// API response describing one member of an organisation.
/// </summary>
public sealed record OrgMemberResponse(
    string UserId,
    string? UserName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt);
