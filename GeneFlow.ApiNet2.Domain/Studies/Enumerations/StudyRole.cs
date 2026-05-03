using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Studies.Enumerations;

/// <summary>
/// Role of a member within a study. Roles are ordered by permission level
/// (higher means more privileges). Permission levels are used to compare
/// roles without coupling callers to specific role identities.
/// </summary>
public sealed class StudyRole : Enumeration<StudyRole>
{
    private const int OwnerPermissionLevel = 100;
    private const int AdminPermissionLevel = 80;
    private const int EditorPermissionLevel = 50;
    private const int ViewerPermissionLevel = 10;

    /// <summary>
    /// Study creator with full control. Can transfer ownership and delete the study.
    /// </summary>
    public static readonly StudyRole Owner = new(1, nameof(Owner), "Owner", OwnerPermissionLevel);

    /// <summary>
    /// Administrator with management rights over members and status, but cannot delete or transfer.
    /// </summary>
    public static readonly StudyRole Admin = new(2, nameof(Admin), "Administrator", AdminPermissionLevel);

    /// <summary>
    /// Member with rights to edit the study content but not to manage members.
    /// </summary>
    public static readonly StudyRole Editor = new(3, nameof(Editor), "Editor", EditorPermissionLevel);

    /// <summary>
    /// Read-only member.
    /// </summary>
    public static readonly StudyRole Viewer = new(4, nameof(Viewer), "Viewer", ViewerPermissionLevel);

    /// <summary>
    /// Human-readable name of the role suitable for UI display.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Numeric permission level used to compare roles. Higher values grant more privileges.
    /// </summary>
    public int PermissionLevel { get; }

    private StudyRole(int id, string name, string displayName, int permissionLevel) : base(id, name)
    {
        DisplayName = displayName;
        PermissionLevel = permissionLevel;
    }

    /// <summary>
    /// Indicates whether the role can add, remove or change roles of other members.
    /// </summary>
    public bool CanManageMembers => this == Owner || this == Admin;

    /// <summary>
    /// Indicates whether the role can edit the study metadata (title, description, settings).
    /// </summary>
    public bool CanEditStudy => PermissionLevel >= Editor.PermissionLevel;

    /// <summary>
    /// Indicates whether the role can edit content owned by the study (papers, tags).
    /// </summary>
    public bool CanEditContent => PermissionLevel >= Editor.PermissionLevel;

    /// <summary>
    /// Indicates whether the role can change the study lifecycle status (draft, published, archived).
    /// </summary>
    public bool CanChangeStatus => this == Owner || this == Admin;

    /// <summary>
    /// Indicates whether the role can soft-delete the study.
    /// </summary>
    public bool CanDeleteStudy => this == Owner;

    /// <summary>
    /// Indicates whether the role can transfer ownership to another member.
    /// </summary>
    public bool CanTransferOwnership => this == Owner;
}
