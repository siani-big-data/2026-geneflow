namespace GeneFlow.ApiNet2.Application.Studies.DTOs;

/// <summary>
/// Study member data transfer object.
/// </summary>
public sealed record StudyMemberDto
{
    public required string UserId { get; init; }
    public required string Role { get; init; }
    public required int RoleId { get; init; }
    public required DateTime JoinedAt { get; init; }
    public string? InvitedBy { get; init; }

    // User profile information
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public string? UserAvatarUrl { get; init; }
}
