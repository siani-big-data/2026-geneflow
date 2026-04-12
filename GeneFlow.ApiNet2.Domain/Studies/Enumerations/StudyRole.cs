using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Studies.Enumerations;

/// <summary>
/// Role of a member within a study.
/// </summary>
public sealed class StudyRole : Enumeration<StudyRole>
{
    public static readonly StudyRole Owner = new(1, nameof(Owner), "Owner", 100);
    public static readonly StudyRole Admin = new(2, nameof(Admin), "Administrator", 80);
    public static readonly StudyRole Editor = new(3, nameof(Editor), "Editor", 50);
    public static readonly StudyRole Viewer = new(4, nameof(Viewer), "Viewer", 10);

    public string DisplayName { get; }
    public int PermissionLevel { get; }

    private StudyRole(int id, string name, string displayName, int permissionLevel) : base(id, name)
    {
        DisplayName = displayName;
        PermissionLevel = permissionLevel;
    }

    public bool CanManageMembers => this == Owner || this == Admin;
    public bool CanEditStudy => PermissionLevel >= Editor.PermissionLevel;
    public bool CanEditContent => PermissionLevel >= Editor.PermissionLevel;
    public bool CanChangeStatus => this == Owner || this == Admin;
    public bool CanDeleteStudy => this == Owner;
    public bool CanTransferOwnership => this == Owner;
}
