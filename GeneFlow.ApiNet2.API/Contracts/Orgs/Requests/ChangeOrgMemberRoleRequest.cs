namespace GeneFlow.ApiNet2.API.Contracts.Orgs.Requests;

/// <summary>
/// Payload for changing the role of an existing org member.
/// </summary>
public sealed record ChangeOrgMemberRoleRequest(string Role);
