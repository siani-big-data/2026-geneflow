using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Orgs.Entities;

/// <summary>
/// A user's membership in an <see cref="Org"/>. Lives inside the Org aggregate
/// and is managed exclusively through the aggregate root.
/// </summary>
public sealed class OrgMember : Entity<Guid>
{
    public OrgId OrgId { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public OrgRole Role { get; private set; } = null!;
    public DateTime JoinedAt { get; private set; }

    private OrgMember() : base() { }

    private OrgMember(Guid id, OrgId orgId, UserId userId, OrgRole role) : base(id)
    {
        OrgId = orgId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
    }

    internal static OrgMember Create(OrgId orgId, UserId userId, OrgRole role)
    {
        return new OrgMember(Guid.NewGuid(), orgId, userId, role);
    }

    internal void ChangeRole(OrgRole newRole)
    {
        Role = newRole;
    }
}
