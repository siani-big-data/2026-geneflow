namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;

/// <summary>
/// Payload for inviting a user to an org by email with a given role.
/// </summary>
public sealed record InviteOrgMemberRequest(string Email, string Role);
