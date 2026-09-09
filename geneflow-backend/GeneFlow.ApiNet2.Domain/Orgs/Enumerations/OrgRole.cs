using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Orgs.Enumerations;

/// <summary>
/// Role of a member within an organisation. Hierarchy: Owner &gt; Admin &gt; Member.
/// </summary>
public sealed class OrgRole : Enumeration<OrgRole>
{
    /// <summary>
    /// Has full control over the organisation including transfer of ownership.
    /// </summary>
    public static readonly OrgRole Owner = new(1, nameof(Owner));

    /// <summary>
    /// Can manage members, settings and content; cannot delete the organisation.
    /// </summary>
    public static readonly OrgRole Admin = new(2, nameof(Admin));

    /// <summary>
    /// Standard member; can participate but cannot manage members or settings.
    /// </summary>
    public static readonly OrgRole Member = new(3, nameof(Member));

    private OrgRole(int id, string name) : base(id, name) { }

    /// <summary>
    /// True for roles that can manage other members (Owner or Admin).
    /// </summary>
    public bool CanManageMembers => this == Owner || this == Admin;

    /// <summary>
    /// True for roles that can edit the org's profile (Owner or Admin).
    /// </summary>
    public bool CanEditOrg => this == Owner || this == Admin;
}
