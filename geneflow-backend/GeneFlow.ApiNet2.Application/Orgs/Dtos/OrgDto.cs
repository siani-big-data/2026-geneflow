namespace GeneFlow.ApiNet2.Application.Orgs.Dtos;

public sealed record OrgDto
{
    public required string Id { get; init; }
    public required string Handle { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? AvatarUrl { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? Location { get; init; }
    public required string Visibility { get; init; }
    public required int MemberCount { get; init; }
    public string? MyRole { get; init; }
    public required DateTime CreatedAt { get; init; }
}
