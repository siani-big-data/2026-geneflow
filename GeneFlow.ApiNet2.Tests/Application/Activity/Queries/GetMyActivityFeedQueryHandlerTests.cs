using GeneFlow.ApiNet2.Application.Activity.Queries.GetMyActivityFeed;
using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.Domain.Activity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeneFlow.ApiNet2.Tests.Application.Activity.Queries;

/// <summary>
/// Unit tests for <see cref="GetMyActivityFeedQueryHandler"/>. Membership lookup
/// and repository fetch are mocked; the handler's job is composition and
/// cursor handling.
/// </summary>
public sealed class GetMyActivityFeedQueryHandlerTests
{
    private readonly IActivityEventRepository _activityRepository = Substitute.For<IActivityEventRepository>();
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly GetMyActivityFeedQueryHandler _handler;

    public GetMyActivityFeedQueryHandlerTests()
    {
        _handler = new GetMyActivityFeedQueryHandler(
            _activityRepository,
            _studyRepository,
            NullLogger<GetMyActivityFeedQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsFailure()
    {
        var query = new GetMyActivityFeedQuery("not-a-user", 20, null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Activity.InvalidUserId");
    }

    [Fact]
    public async Task Handle_InvalidCursor_ReturnsFailure()
    {
        ConfigureMembership([]);
        var query = new GetMyActivityFeedQuery("U00000001", 20, "***not-base64url***");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Activity.InvalidCursor");
        await _activityRepository.DidNotReceiveWithAnyArgs().GetUserFeedAsync(
            default!, default!, default, default, default);
    }

    [Fact]
    public async Task Handle_NoEvents_ReturnsEmptyPageWithoutCursor()
    {
        ConfigureMembership(["S0000001"]);
        ConfigureFeed([]);

        var query = new GetMyActivityFeedQuery("U00000001", 20, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.NextCursor.Should().BeNull();
        result.Value.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FewerEventsThanLimit_DoesNotIssueNextCursor()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var events = new[]
        {
            NewEvent(occurredAt: baseTime.AddSeconds(-1)),
            NewEvent(occurredAt: baseTime.AddSeconds(-2)),
        };
        ConfigureMembership([]);
        ConfigureFeed(events);

        var query = new GetMyActivityFeedQuery("U00000001", 20, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.HasMore.Should().BeFalse();
        result.Value.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FullPage_EmitsNextCursorFromLastEvent()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var events = Enumerable.Range(0, 3)
            .Select(i => NewEvent(occurredAt: baseTime.AddSeconds(-i)))
            .ToArray();
        ConfigureMembership([]);
        ConfigureFeed(events);

        var query = new GetMyActivityFeedQuery("U00000001", 3, null);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.HasMore.Should().BeTrue();
        result.Value.NextCursor.Should().NotBeNullOrWhiteSpace();

        // The cursor must round-trip to the last event's boundary.
        var decoded = ActivityCursor.Decode(result.Value.NextCursor);
        decoded.Should().NotBeNull();
        decoded!.OccurredAt.Should().Be(events[^1].OccurredAt);
        decoded.Id.Should().Be(events[^1].Id.Value);
    }

    [Fact]
    public async Task Handle_ClampsLimitToMax()
    {
        ConfigureMembership([]);
        ConfigureFeed([]);

        var query = new GetMyActivityFeedQuery("U00000001", 9999, null);
        await _handler.Handle(query, CancellationToken.None);

        await _activityRepository.Received(1).GetUserFeedAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            GetMyActivityFeedQueryHandler.MaxLimit,
            Arg.Any<ActivityCursor?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeOrZeroLimit_FallsBackToDefault()
    {
        ConfigureMembership([]);
        ConfigureFeed([]);

        var query = new GetMyActivityFeedQuery("U00000001", 0, null);
        await _handler.Handle(query, CancellationToken.None);

        await _activityRepository.Received(1).GetUserFeedAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            GetMyActivityFeedQueryHandler.DefaultLimit,
            Arg.Any<ActivityCursor?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsVisibleStudyIdsAndDecodedCursor()
    {
        var studyIds = new[] { "S0000001", "S0000002" };
        ConfigureMembership(studyIds);

        var encoded = ActivityCursor.From(DateTimeOffset.UtcNow.AddMinutes(-1), Guid.NewGuid()).Encode();
        ConfigureFeed([]);

        var query = new GetMyActivityFeedQuery("U00000001", 10, encoded);
        await _handler.Handle(query, CancellationToken.None);

        await _activityRepository.Received(1).GetUserFeedAsync(
            "U00000001",
            Arg.Is<IReadOnlyCollection<string>>(s => s.SequenceEqual(studyIds)),
            10,
            Arg.Is<ActivityCursor?>(c => c != null && c.Encode() == encoded),
            Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------------ helpers

    private void ConfigureMembership(string[] studyIds)
    {
        _studyRepository
            .GetVisibleStudyIdsForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(studyIds);
    }

    private void ConfigureFeed(IReadOnlyList<ActivityEvent> events)
    {
        _activityRepository
            .GetUserFeedAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<int>(),
                Arg.Any<ActivityCursor?>(),
                Arg.Any<CancellationToken>())
            .Returns(events);
    }

    private static ActivityEvent NewEvent(DateTimeOffset occurredAt) =>
        ActivityEvent.Create(
            id: ActivityEventId.New(),
            actorUserId: "U00000001",
            verb: ActivityVerb.Created,
            objectType: ActivityObjectType.Study,
            objectId: "S0000001",
            studyId: "S0000001",
            occurredAt: occurredAt,
            visibility: ActivityVisibility.StudyMembers,
            payloadJson: "{}",
            sourceEventType: "TestEvent",
            sourceMessageId: $"msg-{Guid.NewGuid():N}");
}
