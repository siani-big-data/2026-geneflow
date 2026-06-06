using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Profiles.Entities;

/// <summary>
/// A study pinned to a user's profile (showcased on the profile page).
/// Each user can pin up to <see cref="MaxPinnedPerUser"/> studies, ordered by
/// the <see cref="Order"/> field.
/// </summary>
public sealed class PinnedStudy : Entity<Guid>
{
    /// <summary>Maximum number of studies a user can pin to their profile.</summary>
    public const int MaxPinnedPerUser = 6;

    public UserId UserId { get; private set; } = null!;
    public StudyId StudyId { get; private set; } = null!;
    public int Order { get; private set; }
    public DateTime PinnedAt { get; private set; }

    private PinnedStudy() : base() { }

    private PinnedStudy(Guid id, UserId userId, StudyId studyId, int order) : base(id)
    {
        UserId = userId;
        StudyId = studyId;
        Order = order;
        PinnedAt = DateTime.UtcNow;
    }

    public static PinnedStudy Create(UserId userId, StudyId studyId, int order)
    {
        if (order < 0 || order >= MaxPinnedPerUser)
            throw new ArgumentOutOfRangeException(
                nameof(order),
                $"Pin order must be between 0 and {MaxPinnedPerUser - 1}.");

        return new PinnedStudy(Guid.NewGuid(), userId, studyId, order);
    }
}
