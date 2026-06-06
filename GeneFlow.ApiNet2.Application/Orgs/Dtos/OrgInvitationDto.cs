namespace GeneFlow.ApiNet2.Application.Orgs.Dtos;

public sealed record OrgInvitationDto
{
    public required string Id { get; init; }
    public required string OrgId { get; init; }
    public string? OrgHandle { get; init; }
    public string? OrgName { get; init; }
    public required string InvitedEmail { get; init; }
    public required string Role { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    /// <summary>
    /// Opaque accept/decline token. Returned on invite-create so the inviter
    /// can share the link out-of-band; included on list-mine so the recipient
    /// can act on the invitation from the UI.
    /// </summary>
    public required string Token { get; init; }
}
