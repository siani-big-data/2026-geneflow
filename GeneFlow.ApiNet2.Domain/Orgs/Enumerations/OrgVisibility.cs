using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Orgs.Enumerations;

/// <summary>
/// Controls who can see an organisation's profile.
/// </summary>
public sealed class OrgVisibility : Enumeration<OrgVisibility>
{
    /// <summary>
    /// Any anonymous visitor may view the org's profile page.
    /// </summary>
    public static readonly OrgVisibility Public = new(1, nameof(Public));

    /// <summary>
    /// Only members of the org may view its profile page.
    /// </summary>
    public static readonly OrgVisibility Private = new(2, nameof(Private));

    private OrgVisibility(int id, string name) : base(id, name) { }
}
