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
}
