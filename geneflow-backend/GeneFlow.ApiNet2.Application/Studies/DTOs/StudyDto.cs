using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Application.Studies.DTOs;

/// <summary>
/// Current user's permissions within the study.
/// </summary>
public sealed record CurrentUserPermissionsDto
{
    /// <summary>Whether the current user is a member of the study.</summary>
    public required bool IsMember { get; init; }

    /// <summary>The user's role ID (null if not a member).</summary>
    public int? RoleId { get; init; }

    /// <summary>The user's role name (null if not a member).</summary>
    public string? RoleName { get; init; }

    /// <summary>Can manage members (invite, remove, change roles).</summary>
    public required bool CanManageMembers { get; init; }

    /// <summary>Can edit study metadata (title, description, etc.).</summary>
    public required bool CanEditStudy { get; init; }

    /// <summary>Can edit study content (traces, annotations, etc.).</summary>
    public required bool CanEditContent { get; init; }

    /// <summary>Can change study status (draft, active, published, etc.).</summary>
    public required bool CanChangeStatus { get; init; }

    /// <summary>Can delete the study.</summary>
    public required bool CanDeleteStudy { get; init; }

    /// <summary>Can transfer ownership to another member.</summary>
    public required bool CanTransferOwnership { get; init; }

    /// <summary>Creates a read-only permissions object for non-members viewing a public study.</summary>
    public static CurrentUserPermissionsDto ReadOnly => new()
    {
        IsMember = false,
        RoleId = null,
        RoleName = null,
        CanManageMembers = false,
        CanEditStudy = false,
        CanEditContent = false,
        CanChangeStatus = false,
        CanDeleteStudy = false,
        CanTransferOwnership = false
    };

    /// <summary>Creates permissions from a study role.</summary>
    public static CurrentUserPermissionsDto FromRole(StudyRole role) => new()
    {
        IsMember = true,
        RoleId = role.Id,
        RoleName = role.Name,
        CanManageMembers = role.CanManageMembers,
        CanEditStudy = role.CanEditStudy,
        CanEditContent = role.CanEditContent,
        CanChangeStatus = role.CanChangeStatus,
        CanDeleteStudy = role.CanDeleteStudy,
        CanTransferOwnership = role.CanTransferOwnership
    };
}

/// <summary>
/// Full study data transfer object.
/// </summary>
public sealed record StudyDto
{
    public required string Id { get; init; }
    public required string OwnerId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string ResearchField { get; init; }
    public required int ResearchFieldId { get; init; }
    public required string Status { get; init; }
    public required int StatusId { get; init; }

    // Settings
    public required bool AllowPublicComments { get; init; }
    public required bool AllowDataDownload { get; init; }
    public required bool RequireApprovalToJoin { get; init; }

    // New fields
    public string? Institution { get; init; }
    public string? PrincipalInvestigator { get; init; }
    public string? ReadmeMarkdown { get; init; }
    public required bool IsFeatured { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }

    // Metrics
    public required int ViewsCount { get; init; }
    public required int StarsCount { get; init; }

    // Members
    public required IReadOnlyList<StudyMemberDto> Members { get; init; }

    // Papers
    public required IReadOnlyList<StudyPaperDto> Papers { get; init; }

    // Current user permissions
    public required CurrentUserPermissionsDto CurrentUserPermissions { get; init; }

    // Audit
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}
