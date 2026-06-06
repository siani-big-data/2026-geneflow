using GeneFlow.ApiNet2.Application.Search.Queries.Feed;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Application.Search.Queries;

public class FeedQueryHandlerTests
{
    private readonly ISearchIndexRepository _searchIndex = Substitute.For<ISearchIndexRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWatchRepository _watches = Substitute.For<IWatchRepository>();
    private readonly FeedQueryHandler _handler;

    public FeedQueryHandlerTests()
    {
        _users.GetFollowingIdsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserId>());
        _watches.GetWatchedStudyIdsAsync(Arg.Any<UserId>(), Arg.Any<WatchLevel>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudyId>());
        _handler = new FeedQueryHandler(_searchIndex, _users, _watches);
    }

    private static SearchHit Hit(string objectId, string? ownerId,
        SearchObjectType type, DateTime? updatedAt = null) =>
        new(Guid.NewGuid(), type, objectId, ownerId, "T", null, null, true,
            updatedAt ?? DateTime.UtcNow, Rank: 0);

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsNotFoundError()
    {
        var result = await _handler.Handle(
            new FeedQuery("not-a-user", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.NotFound);
    }

    [Fact]
    public async Task Handle_InvalidCursor_ReturnsInvalidCursorError()
    {
        var result = await _handler.Handle(
            new FeedQuery("U00000001", "garbage"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.InvalidCursor);
    }

    [Fact]
    public async Task Handle_TagsItem_AsSelf_WhenOwnerIdMatchesUser()
    {
        const string userIdStr = "U00000001";
        _searchIndex.GetFeedAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                userIdStr,
                Arg.Any<DateTime?>(), Arg.Any<Guid?>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<SearchHit>
            {
                Hit("S0001", userIdStr, SearchObjectType.Study),
            });

        var result = await _handler.Handle(
            new FeedQuery(userIdStr, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Reason.Should().Be("self");
    }

    [Fact]
    public async Task Handle_TagsItem_AsFollow_WhenOwnerIsFollowed()
    {
        var userId = new UserId(1);
        var authorId = new UserId(2);
        var userIdStr = userId.ToString();
        var authorIdStr = authorId.ToString();

        _users.GetFollowingIdsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserId> { authorId });

        _searchIndex.GetFeedAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                userIdStr,
                Arg.Any<DateTime?>(), Arg.Any<Guid?>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<SearchHit>
            {
                Hit("S0002", authorIdStr, SearchObjectType.Study),
            });

        var result = await _handler.Handle(
            new FeedQuery(userIdStr, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Reason.Should().Be("follow");
    }

    [Fact]
    public async Task Handle_TagsItem_AsWatch_WhenStudyIsWatched()
    {
        var userId = new UserId(1);
        var studyId = new StudyId(123);
        var userIdStr = userId.ToString();
        var studyIdStr = studyId.ToString();

        _watches.GetWatchedStudyIdsAsync(Arg.Any<UserId>(), Arg.Any<WatchLevel>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudyId> { studyId });

        _searchIndex.GetFeedAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                userIdStr,
                Arg.Any<DateTime?>(), Arg.Any<Guid?>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<SearchHit>
            {
                Hit(studyIdStr, "U00000099", SearchObjectType.Study),
            });

        var result = await _handler.Handle(
            new FeedQuery(userIdStr, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Reason.Should().Be("watch");
    }
}
