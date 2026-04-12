using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Studies.Entities;

/// <summary>
/// Represents a user starring/favoriting a study.
/// </summary>
public sealed class StudyStar : Entity<Guid>
{
    public StudyId StudyId { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public DateTime StarredAt { get; private set; }

    private StudyStar() : base() { }

    private StudyStar(Guid id, StudyId studyId, UserId userId) : base(id)
    {
        StudyId = studyId;
        UserId = userId;
        StarredAt = DateTime.UtcNow;
    }

    public static StudyStar Create(StudyId studyId, UserId userId)
    {
        return new StudyStar(Guid.NewGuid(), studyId, userId);
    }
}
