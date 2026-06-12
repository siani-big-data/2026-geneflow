using GeneFlow.ApiNet2.Application.Discussions.Commands.CreateComment;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Entities;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Application.Discussions.Commands;

public class CreateCommentCommandHandlerTests
{
    private readonly IDiscussionRepository _discussionRepo = Substitute.For<IDiscussionRepository>();
    private readonly ICommentRepository _commentRepo = Substitute.For<ICommentRepository>();
    private readonly IDiscussionUnitOfWork _uow = Substitute.For<IDiscussionUnitOfWork>();
    private readonly IWatchRepository _watchRepo = Substitute.For<IWatchRepository>();
    private readonly INotificationUnitOfWork _notificationUow = Substitute.For<INotificationUnitOfWork>();
    private readonly CreateCommentCommandHandler _handler;

    public CreateCommentCommandHandlerTests()
    {
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _notificationUow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _watchRepo.GetAsync(Arg.Any<UserId>(), Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Watch?>(null));
        _handler = new CreateCommentCommandHandler(
            _discussionRepo, _commentRepo, _uow, _watchRepo, _notificationUow);
    }

    private static Discussion BuildDiscussion(bool locked = false)
    {
        var d = Discussion.Create(new DiscussionId(1), new StudyId(1), new UserId(1), "T", null).Value;
        if (locked)
            d.Lock(new UserId(1));
        return d;
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldCreateCommentAndSave()
    {
        var discussion = BuildDiscussion();
        _discussionRepo.GetByIdAsync(Arg.Any<DiscussionId>(), Arg.Any<CancellationToken>()).Returns(discussion);

        var command = new CreateCommentCommand("S00000001", "D00000001", "U00000002", "Hello");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BodyMarkdown.Should().Be("Hello");
        await _commentRepo.Received(1).AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidDiscussionId_ShouldFail()
    {
        var command = new CreateCommentCommand("S00000001", "not-a-discussion", "U00000002", "Hello");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _commentRepo.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DiscussionNotFound_ShouldFail()
    {
        _discussionRepo.GetByIdAsync(Arg.Any<DiscussionId>(), Arg.Any<CancellationToken>())
            .Returns((Discussion?)null);

        var command = new CreateCommentCommand("S00000001", "D00000001", "U00000002", "Hello");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_LockedDiscussion_ShouldFail()
    {
        _discussionRepo.GetByIdAsync(Arg.Any<DiscussionId>(), Arg.Any<CancellationToken>())
            .Returns(BuildDiscussion(locked: true));

        var command = new CreateCommentCommand("S00000001", "D00000001", "U00000002", "Hello");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DiscussionErrors.Locked);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyBody_ShouldFail()
    {
        _discussionRepo.GetByIdAsync(Arg.Any<DiscussionId>(), Arg.Any<CancellationToken>())
            .Returns(BuildDiscussion());

        var command = new CreateCommentCommand("S00000001", "D00000001", "U00000002", "   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _commentRepo.DidNotReceive().AddAsync(Arg.Any<Comment>(), Arg.Any<CancellationToken>());
    }
}
