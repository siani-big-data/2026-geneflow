namespace GeneFlow.ApiNet2.Application.Studies.DTOs;

/// <summary>
/// Study invitation data transfer object.
/// </summary>
public sealed record StudyInvitationDto
{
    public required string Id { get; init; }
    public required string StudyId { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public required int RoleId { get; init; }
    public required string Status { get; init; }
    public required int StatusId { get; init; }
    public required string InvitedBy { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public DateTime? RespondedAt { get; init; }
    public string? Message { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
