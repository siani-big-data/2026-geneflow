using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Discussions;

/// <summary>
/// A discussion thread anchored to a Study. Discussion does NOT own its
/// comments — comments are independent aggregates referencing the
/// discussion polymorphically. Phase 4 keeps the aggregate intentionally
/// small so comment scaling does not require an aggregate-wide lock.
/// </summary>
public sealed class Discussion : FullAuditableAggregateRoot<DiscussionId>
{
    public const int MaxTitleLength = 200;
    public const int MaxCategoryLength = 50;

    public StudyId StudyId { get; private set; } = null!;
    public UserId AuthorId { get; private set; } = null!;
    public string Title { get; private set; } = null!;

    /// <summary>
    /// Free-form category label (e.g. "Question", "Idea"). Optional;
    /// not modeled as an enumeration so individual studies / future
    /// custom labels don't require a migration.
    /// </summary>
    public string? Category { get; private set; }

    public bool IsLocked { get; private set; }
    public DateTime? LockedAt { get; private set; }
    public UserId? LockedBy { get; private set; }

    private Discussion() : base() { }

    private Discussion(
        DiscussionId id,
        StudyId studyId,
        UserId authorId,
        string title,
        string? category) : base(id)
    {
        StudyId = studyId;
        AuthorId = authorId;
        Title = title;
        Category = category;
        IsLocked = false;

        InitializeCreatedAt(authorId.ToString());
    }

    public static Result<Discussion> Create(
        DiscussionId id,
        StudyId studyId,
        UserId authorId,
        string title,
        string? category)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<Discussion>(DiscussionErrors.TitleRequired);

        if (title.Length > MaxTitleLength)
            return Result.Failure<Discussion>(DiscussionErrors.TitleTooLong);

        if (category is { Length: > MaxCategoryLength })
            return Result.Failure<Discussion>(DiscussionErrors.CategoryTooLong);

        var discussion = new Discussion(id, studyId, authorId, title.Trim(), category?.Trim());

        discussion.RaiseDomainEvent(new DiscussionCreatedEvent(
            discussion.Id,
            discussion.StudyId,
            discussion.AuthorId,
            discussion.Title));

        return discussion;
    }

    public Result Lock(UserId lockedBy)
    {
        if (IsLocked)
            return Result.Failure(DiscussionErrors.AlreadyLocked);

        IsLocked = true;
        LockedAt = DateTime.UtcNow;
        LockedBy = lockedBy;

        SetModified(lockedBy.ToString());
        RaiseDomainEvent(new DiscussionLockedEvent(Id, lockedBy));
        return Result.Success();
    }

    public Result Unlock(UserId unlockedBy)
    {
        if (!IsLocked)
            return Result.Failure(DiscussionErrors.NotLocked);

        IsLocked = false;
        LockedAt = null;
        LockedBy = null;

        SetModified(unlockedBy.ToString());
        RaiseDomainEvent(new DiscussionUnlockedEvent(Id, unlockedBy));
        return Result.Success();
    }
}
