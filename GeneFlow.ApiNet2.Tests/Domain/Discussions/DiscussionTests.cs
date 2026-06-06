using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Domain.Discussions;

public class DiscussionTests
{
    private static DiscussionId NewDiscussionId(long v = 1) => new(v);
    private static StudyId NewStudyId(long v = 1) => new(v);
    private static UserId NewUserId(long v = 1) => new(v);

    [Fact]
    public void Create_WithValidData_ShouldSucceedAndRaiseEvent()
    {
        var result = Discussion.Create(NewDiscussionId(), NewStudyId(), NewUserId(), "  Title  ", "Question");

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Title");
        result.Value.Category.Should().Be("Question");
        result.Value.IsLocked.Should().BeFalse();
        result.Value.DomainEvents.Should().ContainSingle(e => e is DiscussionCreatedEvent);
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldFail()
    {
        var result = Discussion.Create(NewDiscussionId(), NewStudyId(), NewUserId(), "   ", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DiscussionErrors.TitleRequired);
    }

    [Fact]
    public void Create_WithTooLongTitle_ShouldFail()
    {
        var longTitle = new string('a', Discussion.MaxTitleLength + 1);

        var result = Discussion.Create(NewDiscussionId(), NewStudyId(), NewUserId(), longTitle, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DiscussionErrors.TitleTooLong);
    }

    [Fact]
    public void Lock_OnUnlocked_ShouldLockAndRaiseEvent()
    {
        var discussion = Discussion.Create(NewDiscussionId(), NewStudyId(), NewUserId(1), "T", null).Value;
        discussion.ClearDomainEvents();

        var result = discussion.Lock(NewUserId(2));

        result.IsSuccess.Should().BeTrue();
        discussion.IsLocked.Should().BeTrue();
        discussion.LockedBy.Should().Be(NewUserId(2));
        discussion.LockedAt.Should().NotBeNull();
        discussion.DomainEvents.Should().ContainSingle(e => e is DiscussionLockedEvent);
    }

    [Fact]
    public void Lock_WhenAlreadyLocked_ShouldFail()
    {
        var discussion = Discussion.Create(NewDiscussionId(), NewStudyId(), NewUserId(1), "T", null).Value;
        discussion.Lock(NewUserId(2));

        var second = discussion.Lock(NewUserId(2));

        second.IsFailure.Should().BeTrue();
        second.Error.Should().Be(DiscussionErrors.AlreadyLocked);
    }

    [Fact]
    public void Unlock_WhenNotLocked_ShouldFail()
    {
        var discussion = Discussion.Create(NewDiscussionId(), NewStudyId(), NewUserId(1), "T", null).Value;

        var result = discussion.Unlock(NewUserId(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DiscussionErrors.NotLocked);
    }

    [Fact]
    public void Unlock_AfterLock_ShouldUnlockAndRaiseEvent()
    {
        var discussion = Discussion.Create(NewDiscussionId(), NewStudyId(), NewUserId(1), "T", null).Value;
        discussion.Lock(NewUserId(2));
        discussion.ClearDomainEvents();

        var result = discussion.Unlock(NewUserId(2));

        result.IsSuccess.Should().BeTrue();
        discussion.IsLocked.Should().BeFalse();
        discussion.LockedBy.Should().BeNull();
        discussion.LockedAt.Should().BeNull();
        discussion.DomainEvents.Should().ContainSingle(e => e is DiscussionUnlockedEvent);
    }
}
