using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Notifications.Events;

namespace GeneFlow.ApiNet2.Tests.Domain.Notifications;

public class NotificationTests
{
    private static Notification NewNotification(UserId? recipient = null) =>
        Notification.Create(
            new NotificationId(1),
            recipient ?? new UserId(1),
            NotificationType.CommentCreated,
            "Subject",
            "/url").Value;

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = Notification.Create(
            new NotificationId(1), new UserId(1),
            NotificationType.CommentCreated, "  hello  ", "/u");

        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be("hello");
        result.Value.IsRead.Should().BeFalse();
        result.Value.DomainEvents.Should().ContainSingle(e => e is NotificationCreatedEvent);
    }

    [Fact]
    public void Create_WithEmptySubject_ShouldFail()
    {
        var result = Notification.Create(new NotificationId(1), new UserId(1),
            NotificationType.CommentCreated, "  ", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.SubjectRequired);
    }

    [Fact]
    public void MarkRead_ByRecipient_ShouldSetIsRead()
    {
        var n = NewNotification(new UserId(1));
        n.ClearDomainEvents();

        var result = n.MarkRead(new UserId(1));

        result.IsSuccess.Should().BeTrue();
        n.IsRead.Should().BeTrue();
        n.ReadAt.Should().NotBeNull();
        n.DomainEvents.Should().ContainSingle(e => e is NotificationReadEvent);
    }

    [Fact]
    public void MarkRead_ByNonRecipient_ShouldFail()
    {
        var n = NewNotification(new UserId(1));

        var result = n.MarkRead(new UserId(2));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotRecipient);
        n.IsRead.Should().BeFalse();
    }

    [Fact]
    public void MarkRead_TwiceByRecipient_ShouldBeIdempotent()
    {
        var n = NewNotification(new UserId(1));
        n.MarkRead(new UserId(1));
        var firstReadAt = n.ReadAt;
        n.ClearDomainEvents();

        var result = n.MarkRead(new UserId(1));

        result.IsSuccess.Should().BeTrue();
        n.IsRead.Should().BeTrue();
        n.ReadAt.Should().Be(firstReadAt);
        n.DomainEvents.Should().BeEmpty();
    }
}
