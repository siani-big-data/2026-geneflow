using GeneFlow.ApiNet2.Application.Notifications.EventHandlers;
using GeneFlow.ApiNet2.Application.Notifications.SSE;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeneFlow.ApiNet2.Tests.Application.Notifications.EventHandlers;

public class WatchNotifier_OnCommentCreatedTests
{
    private readonly IDiscussionRepository _discussionRepo = Substitute.For<IDiscussionRepository>();
    private readonly IWatchRepository _watchRepo = Substitute.For<IWatchRepository>();
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();
    private readonly INotificationUnitOfWork _uow = Substitute.For<INotificationUnitOfWork>();
    private readonly INotificationEventBroker _broker = Substitute.For<INotificationEventBroker>();
    private readonly WatchNotifier_OnCommentCreated _handler;

    public WatchNotifier_OnCommentCreatedTests()
    {
        _handler = new WatchNotifier_OnCommentCreated(
            _discussionRepo, _watchRepo, _notificationRepo, _uow, _broker,
            NullLogger<WatchNotifier_OnCommentCreated>.Instance);
    }

    private static Discussion BuildDiscussion(UserId author) =>
        Discussion.Create(new DiscussionId(1), new StudyId(42), author, "Title", null).Value;

    private static CommentCreatedEvent NewEvent(UserId author) =>
        new(Guid.NewGuid(), CommentParentType.Discussion, "D00000001", author, "Hello there");

    [Fact]
    public async Task Handle_NonDiscussionParent_ShouldDoNothing()
    {
        var evt = new CommentCreatedEvent(
            Guid.NewGuid(),
            CommentParentType.Annotation,
            "ignored",
            new UserId(1),
            "Hello");

        await _handler.Handle(evt, CancellationToken.None);

        await _watchRepo.DidNotReceive().GetWatchersAsync(
            Arg.Any<StudyId>(), Arg.Any<WatchLevel>(), Arg.Any<CancellationToken>());
        await _notificationRepo.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DiscussionNotFound_ShouldSkip()
    {
        _discussionRepo.GetByIdAsync(Arg.Any<DiscussionId>(), Arg.Any<CancellationToken>())
            .Returns((Discussion?)null);

        await _handler.Handle(NewEvent(new UserId(1)), CancellationToken.None);

        await _notificationRepo.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWatchers_ShouldCreateOnePerWatcher_ExceptAuthor_AndPublish()
    {
        var author = new UserId(1);
        var watcher1 = new UserId(2);
        var watcher2 = new UserId(3);

        _discussionRepo.GetByIdAsync(Arg.Any<DiscussionId>(), Arg.Any<CancellationToken>())
            .Returns(BuildDiscussion(author));

        _watchRepo.GetWatchersAsync(Arg.Any<StudyId>(), WatchLevel.All, Arg.Any<CancellationToken>())
            .Returns(new[] { watcher1, watcher2, author });

        // sequence generator returns 100, 101 for the two recipients
        var seq = 100L;
        _notificationRepo.GetNextSequenceValueAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(seq++));

        await _handler.Handle(NewEvent(author), CancellationToken.None);

        await _notificationRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Notification>>(c => c.Count() == 2 &&
                                                  c.All(n => n.RecipientId != author)),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _broker.Received(1).PublishAsync(
            watcher1.ToString()!, Arg.Any<NotificationEventDto>(), Arg.Any<CancellationToken>());
        await _broker.Received(1).PublishAsync(
            watcher2.ToString()!, Arg.Any<NotificationEventDto>(), Arg.Any<CancellationToken>());
        await _broker.DidNotReceive().PublishAsync(
            author.ToString()!, Arg.Any<NotificationEventDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyAuthorWatches_ShouldNotPublish()
    {
        var author = new UserId(1);

        _discussionRepo.GetByIdAsync(Arg.Any<DiscussionId>(), Arg.Any<CancellationToken>())
            .Returns(BuildDiscussion(author));

        _watchRepo.GetWatchersAsync(Arg.Any<StudyId>(), WatchLevel.All, Arg.Any<CancellationToken>())
            .Returns(new[] { author });

        await _handler.Handle(NewEvent(author), CancellationToken.None);

        await _notificationRepo.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<Notification>>(), Arg.Any<CancellationToken>());
        await _broker.DidNotReceive().PublishAsync(
            Arg.Any<string>(), Arg.Any<NotificationEventDto>(), Arg.Any<CancellationToken>());
    }
}
