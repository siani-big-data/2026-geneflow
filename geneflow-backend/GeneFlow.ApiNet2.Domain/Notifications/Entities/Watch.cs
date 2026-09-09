using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Notifications.Events;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Notifications.Entities;

/// <summary>
/// A user's subscription to a study. Uniqueness enforced on
/// (UserId, StudyId) at the database level.
/// </summary>
public sealed class Watch : AggregateRoot<Guid>
{
    public UserId UserId { get; private set; } = null!;
    public StudyId StudyId { get; private set; } = null!;
    public WatchLevel Level { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private Watch() : base() { }

    private Watch(Guid id, UserId userId, StudyId studyId, WatchLevel level) : base(id)
    {
        UserId = userId;
        StudyId = studyId;
        Level = level;
        CreatedAt = DateTime.UtcNow;
    }

    public static Watch Create(UserId userId, StudyId studyId, WatchLevel level)
    {
        var watch = new Watch(Guid.NewGuid(), userId, studyId, level);
        watch.RaiseDomainEvent(new WatchUpdatedEvent(watch.Id, userId, studyId, level));
        return watch;
    }

    public void SetLevel(WatchLevel level)
    {
        if (Level == level)
            return;
        Level = level;
        ModifiedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WatchUpdatedEvent(Id, UserId, StudyId, level));
    }
}
