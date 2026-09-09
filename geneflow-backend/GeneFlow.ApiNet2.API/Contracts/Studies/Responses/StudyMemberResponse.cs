namespace GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

/// <summary>
/// Response model for study member information.
/// </summary>
public sealed record StudyMemberResponse(
    string UserId,
    int RoleId,
    string RoleName,
    DateTime JoinedAt,
    string? InvitedBy,
    string? UserName,
    string? UserEmail,
    string? UserAvatarUrl);
