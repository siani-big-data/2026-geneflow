using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.Domain.Activity.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Activity.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Activity.Persistence.Repositories;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;
using GeneFlow.ApiNet2.Tests.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Activity;

/// <summary>
/// Integration tests for the activity event repository.
/// Verifies idempotency, keyset pagination stability and visibility filtering.
/// </summary>
public sealed class ActivityEventRepositoryTests : RepositoryTestBase
{
    public ActivityEventRepositoryTests(
        PostgreSqlContainerFixture postgresFixture,
        RedisContainerFixture redisFixture)
        : base(postgresFixture, redisFixture)
    {
    }

    protected override async Task PrepareSchemaAsync()
    {
        await EnsureMigratedAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsActivityEvent()
    {
        await using var context = CreateContext();
        var repository = new ActivityEventRepository(context, CreateLogger<ActivityEventRepository>());

        var entity = NewEvent(
            verb: ActivityVerb.Created,
            objectType: ActivityObjectType.Study,
            objectId: "S0000001",
            actorUserId: "U0000001",
            studyId: "S0000001",
            occurredAt: DateTimeOffset.UtcNow,
            visibility: ActivityVisibility.StudyMembers,
            sourceMessageId: $"msg-{Guid.NewGuid():N}");

        await repository.AddAsync(entity, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        var stored = await repository.GetByIdAsync(entity.Id, CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.Verb.Should().Be(ActivityVerb.Created);
        stored.ObjectType.Should().Be(ActivityObjectType.Study);
        stored.ActorUserId.Should().Be("U0000001");
    }

    [Fact]
    public async Task ExistsBySourceMessageId_DistinguishesProjectedFromMissing()
    {
        await using var context = CreateContext();
        var repository = new ActivityEventRepository(context, CreateLogger<ActivityEventRepository>());

        var msgId = $"msg-{Guid.NewGuid():N}";
        var entity = NewEvent(sourceMessageId: msgId);
        await repository.AddAsync(entity, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        (await repository.ExistsBySourceMessageIdAsync(msgId, CancellationToken.None))
            .Should().BeTrue();
        (await repository.ExistsBySourceMessageIdAsync($"missing-{Guid.NewGuid():N}", CancellationToken.None))
            .Should().BeFalse();
    }

    [Fact]
    public async Task GetUserFeed_ReturnsActorEventsAndPublicEvents()
    {
        await using var context = CreateContext();
        var repository = new ActivityEventRepository(context, CreateLogger<ActivityEventRepository>());

        var actor = $"U-{Guid.NewGuid():N}"[..16];
        var someoneElse = $"U-{Guid.NewGuid():N}"[..16];
        var now = DateTimeOffset.UtcNow;

        await SeedAsync(repository,
            NewEvent(actorUserId: actor, occurredAt: now.AddMinutes(-1),
                visibility: ActivityVisibility.Private,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"),
            NewEvent(actorUserId: someoneElse, occurredAt: now.AddMinutes(-2),
                visibility: ActivityVisibility.Public, objectType: ActivityObjectType.User,
                verb: ActivityVerb.Registered,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"),
            NewEvent(actorUserId: someoneElse, occurredAt: now.AddMinutes(-3),
                visibility: ActivityVisibility.Private,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"));

        var feed = await repository.GetUserFeedAsync(
            actor,
            visibleStudyIds: [],
            limit: 50,
            cursor: null,
            CancellationToken.None);

        feed.Should().HaveCount(2);
        feed.Select(e => e.ActorUserId).Should().BeEquivalentTo(actor, someoneElse);
    }

    [Fact]
    public async Task GetUserFeed_IncludesStudyScopedEventsForVisibleStudies()
    {
        await using var context = CreateContext();
        var repository = new ActivityEventRepository(context, CreateLogger<ActivityEventRepository>());

        var actor = $"U-{Guid.NewGuid():N}"[..16];
        var visibleStudy = $"S-{Guid.NewGuid():N}"[..16];
        var hiddenStudy = $"S-{Guid.NewGuid():N}"[..16];
        var now = DateTimeOffset.UtcNow;

        await SeedAsync(repository,
            NewEvent(actorUserId: "other", studyId: visibleStudy, occurredAt: now.AddMinutes(-1),
                visibility: ActivityVisibility.StudyMembers,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"),
            NewEvent(actorUserId: "other", studyId: hiddenStudy, occurredAt: now.AddMinutes(-2),
                visibility: ActivityVisibility.StudyMembers,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"));

        var feed = await repository.GetUserFeedAsync(
            actor,
            visibleStudyIds: [visibleStudy],
            limit: 50,
            cursor: null,
            CancellationToken.None);

        feed.Should().HaveCount(1);
        feed[0].StudyId.Should().Be(visibleStudy);
    }

    [Fact]
    public async Task GetStudyTimeline_FiltersByStudyAndExcludesPrivate()
    {
        await using var context = CreateContext();
        var repository = new ActivityEventRepository(context, CreateLogger<ActivityEventRepository>());

        var study = $"S-{Guid.NewGuid():N}"[..16];
        var other = $"S-{Guid.NewGuid():N}"[..16];
        var now = DateTimeOffset.UtcNow;

        await SeedAsync(repository,
            NewEvent(studyId: study, occurredAt: now.AddMinutes(-1),
                visibility: ActivityVisibility.StudyMembers,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"),
            NewEvent(studyId: study, occurredAt: now.AddMinutes(-2),
                visibility: ActivityVisibility.Private,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"),
            NewEvent(studyId: other, occurredAt: now.AddMinutes(-3),
                visibility: ActivityVisibility.StudyMembers,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"));

        var timeline = await repository.GetStudyTimelineAsync(
            study,
            limit: 50,
            cursor: null,
            CancellationToken.None);

        timeline.Should().HaveCount(1);
        timeline[0].StudyId.Should().Be(study);
        timeline[0].Visibility.Should().Be(ActivityVisibility.StudyMembers);
    }

    [Fact]
    public async Task KeysetPagination_ProducesStablePages()
    {
        await using var context = CreateContext();
        var repository = new ActivityEventRepository(context, CreateLogger<ActivityEventRepository>());

        var actor = $"U-{Guid.NewGuid():N}"[..16];
        var baseTime = DateTimeOffset.UtcNow;
        var toSeed = Enumerable.Range(0, 7)
            .Select(i => NewEvent(
                actorUserId: actor,
                occurredAt: baseTime.AddSeconds(-i),
                visibility: ActivityVisibility.Private,
                sourceMessageId: $"msg-{Guid.NewGuid():N}"))
            .ToArray();

        await SeedAsync(repository, toSeed);

        var page1 = await repository.GetAuditLogAsync(actor, limit: 3, cursor: null, CancellationToken.None);
        page1.Should().HaveCount(3);

        var lastOfPage1 = page1[^1];
        var cursor = ActivityCursor.From(lastOfPage1.OccurredAt, lastOfPage1.Id.Value);
        var page2 = await repository.GetAuditLogAsync(actor, limit: 3, cursor: cursor, CancellationToken.None);
        page2.Should().HaveCount(3);

        page2.Select(e => e.Id).Should().NotIntersectWith(page1.Select(e => e.Id));

        var lastOfPage2 = page2[^1];
        var cursor2 = ActivityCursor.From(lastOfPage2.OccurredAt, lastOfPage2.Id.Value);
        var page3 = await repository.GetAuditLogAsync(actor, limit: 3, cursor: cursor2, CancellationToken.None);
        page3.Should().HaveCount(1);
    }

    [Fact]
    public async Task UniqueIndex_PreventsDuplicateSourceMessageIds()
    {
        await using var context = CreateContext();
        var repository = new ActivityEventRepository(context, CreateLogger<ActivityEventRepository>());

        var msgId = $"msg-{Guid.NewGuid():N}";
        await repository.AddAsync(NewEvent(sourceMessageId: msgId), CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        await using var context2 = CreateContext();
        var repository2 = new ActivityEventRepository(context2, CreateLogger<ActivityEventRepository>());
        await repository2.AddAsync(NewEvent(sourceMessageId: msgId), CancellationToken.None);

        var act = async () => await repository2.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // ----------------------------- helpers -----------------------------

    private ActivityContext CreateContext()
        => new(CreateDbContextOptions<ActivityContext>());

    private async Task EnsureMigratedAsync()
    {
        await using var context = CreateContext();
        await EnsureContextSchemaAsync(context);
    }

    private static async Task SeedAsync(ActivityEventRepository repository, params ActivityEvent[] events)
    {
        foreach (var e in events)
        {
            await repository.AddAsync(e, CancellationToken.None);
        }
        await repository.SaveChangesAsync(CancellationToken.None);
    }

    private static ActivityEvent NewEvent(
        ActivityVerb? verb = null,
        ActivityObjectType? objectType = null,
        string objectId = "S0000001",
        string? actorUserId = "U0000001",
        string? studyId = null,
        DateTimeOffset? occurredAt = null,
        ActivityVisibility? visibility = null,
        string? sourceMessageId = null)
    {
        return ActivityEvent.Create(
            id: ActivityEventId.New(),
            actorUserId: actorUserId,
            verb: verb ?? ActivityVerb.Created,
            objectType: objectType ?? ActivityObjectType.Study,
            objectId: objectId,
            studyId: studyId,
            occurredAt: occurredAt ?? DateTimeOffset.UtcNow,
            visibility: visibility ?? ActivityVisibility.StudyMembers,
            payloadJson: "{}",
            sourceEventType: "TestEvent",
            sourceMessageId: sourceMessageId);
    }
}
