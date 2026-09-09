namespace GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

/// <summary>
/// Response model for study invitation information.
/// </summary>
public sealed record StudyInvitationResponse(
    string Id,
    string StudyId,
    string Email,
    int RoleId,
    string RoleName,
    int StatusId,
    string StatusName,
    string? Token,
    string InvitedBy,
    DateTime ExpiresAt,
    DateTime? RespondedAt,
    string? Message,
    bool IsExpired,
    bool IsPending,
    DateTime CreatedAt);
