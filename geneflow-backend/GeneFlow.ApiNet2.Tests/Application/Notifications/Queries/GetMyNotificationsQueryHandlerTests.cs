using GeneFlow.ApiNet2.Application.Notifications.Queries.GetMyNotifications;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Tests.Application.Notifications.Queries;

public class GetMyNotificationsQueryHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly GetMyNotificationsQueryHandler _handler;

    public GetMyNotificationsQueryHandlerTests()
    {
        _handler = new GetMyNotificationsQueryHandler(_repository);
    }

    private static Notification NewNotification(int seq, UserId recipient) =>
        Notification.Create(new NotificationId(seq), recipient, NotificationType.CommentCreated,
            $"Subject {seq}", "/u").Value;

    [Fact]
    public async Task Handle_InvalidUserId_ShouldFail()
    {
        var result = await _handler.Handle(new GetMyNotificationsQuery("not-a-user"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidUser_ShouldReturnMappedPage()
    {
        var recipient = new UserId(7);
        var items = new[] { NewNotification(1, recipient), NewNotification(2, recipient) };
        var page = PagedList<Notification>.Create(items, 1, 20, 2);

        _repository.GetForUserAsync(Arg.Any<UserId>(), 1, 20, false, Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _handler.Handle(new GetMyNotificationsQuery(recipient.ToString()!), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UnreadOnly_ShouldPassFlagToRepository()
    {
        var recipient = new UserId(7);
        _repository.GetForUserAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(PagedList<Notification>.Create(Array.Empty<Notification>(), 1, 20, 0));

        await _handler.Handle(
            new GetMyNotificationsQuery(recipient.ToString()!, UnreadOnly: true, PageNumber: 2, PageSize: 50),
            CancellationToken.None);

        await _repository.Received(1).GetForUserAsync(
            Arg.Is<UserId>(u => u == recipient), 2, 50, true, Arg.Any<CancellationToken>());
    }
}
