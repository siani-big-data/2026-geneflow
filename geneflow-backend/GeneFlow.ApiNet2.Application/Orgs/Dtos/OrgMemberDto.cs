namespace GeneFlow.ApiNet2.Application.Orgs.Dtos;

public sealed record OrgMemberDto
{
    public required string UserId { get; init; }
    public string? UserName { get; init; }
    public string? AvatarUrl { get; init; }
    public required string Role { get; init; }
    public required DateTime JoinedAt { get; init; }
}
