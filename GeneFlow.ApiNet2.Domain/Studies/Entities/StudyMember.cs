using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Studies.Entities;

/// <summary>
/// Represents a member of a study with their role.
/// </summary>
public sealed class StudyMember : Entity<Guid>
{
    public UserId UserId { get; private set; }
    public StudyRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public UserId? InvitedBy { get; private set; }

    private StudyMember() : base()
    {
        UserId = null!;
        Role = null!;
    }

    private StudyMember(
        Guid id,
        UserId userId,
        StudyRole role,
        UserId? invitedBy) : base(id)
    {
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
        InvitedBy = invitedBy;
    }

    internal static StudyMember Create(
        UserId userId,
        StudyRole role,
        UserId? invitedBy = null)
    {
        return new StudyMember(Guid.NewGuid(), userId, role, invitedBy);
    }

    internal void ChangeRole(StudyRole newRole)
    {
        Role = newRole;
    }
}
