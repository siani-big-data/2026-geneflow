namespace GeneFlow.ApiNet2.Application.Orgs.Dtos;

public sealed record OrgMembershipDto
{
    public required string OrgId { get; init; }
    public required string Handle { get; init; }
    public required string Name { get; init; }
    public string? AvatarUrl { get; init; }
    public required string Role { get; init; }
}
