using FluentAssertions;
using GeneFlow.ApiNet2.Domain.Activity.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Activity.Projection;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Activity;

/// <summary>
/// Unit tests for the convention-based activity projector.
/// </summary>
public sealed class ConventionActivityEventProjectorTests
{
    private readonly ConventionActivityEventProjector _projector =
        new(NullLogger<ConventionActivityEventProjector>.Instance);

    [Fact]
    public void Project_StudyCreated_MapsObjectAndVerb()
    {
        var message = new EventMessage(
            MessageId: "msg-1",
            EventId: "evt-1",
            EventType: "StudyCreatedEvent",
            OccurredAt: DateTime.UtcNow,
            Data: """{"id":"S0000001","ownerId":"U0000007"}""");

        var result = _projector.Project(message, "studies");

        result.Should().NotBeNull();
        result!.ObjectType.Should().Be(ActivityObjectType.Study);
        result.Verb.Should().Be(ActivityVerb.Created);
        result.ObjectId.Should().Be("S0000001");
        result.ActorUserId.Should().Be("U0000007");
        result.SourceMessageId.Should().Be("msg-1");
        result.SourceEventType.Should().Be("StudyCreatedEvent");
    }

    [Fact]
    public void Project_TraceUploaded_ExtractsStudyAndUploader()
    {
        var message = new EventMessage(
            MessageId: "msg-2",
            EventId: "evt-2",
            EventType: "TraceUploadedEvent",
            OccurredAt: DateTime.UtcNow,
            Data: """{"id":"T-abc","uploadedBy":"U-123","studyId":"S0000002"}""");

        var result = _projector.Project(message, "traces");

        result.Should().NotBeNull();
        result!.ObjectType.Should().Be(ActivityObjectType.Trace);
        result.Verb.Should().Be(ActivityVerb.Uploaded);
        result.ActorUserId.Should().Be("U-123");
        result.StudyId.Should().Be("S0000002");
        result.Visibility.Should().Be(ActivityVisibility.StudyMembers);
    }

    [Fact]
    public void Project_PipelineExecutionStarted_MapsCompoundObjectType()
    {
        var message = new EventMessage(
            MessageId: "msg-3",
            EventId: "evt-3",
            EventType: "PipelineExecutionStartedEvent",
            OccurredAt: DateTime.UtcNow,
            Data: """{"id":"PX-1","studyId":"S0000003"}""");

        var result = _projector.Project(message, "pipelines");

        result.Should().NotBeNull();
        result!.ObjectType.Should().Be(ActivityObjectType.PipelineExecution);
        result.Verb.Should().Be(ActivityVerb.Started);
    }

    [Fact]
    public void Project_UserRegistered_DefaultsToPublicVisibility()
    {
        var message = new EventMessage(
            MessageId: "msg-4",
            EventId: "evt-4",
            EventType: "UserRegisteredEvent",
            OccurredAt: DateTime.UtcNow,
            Data: """{"id":"U0001","userId":"U0001"}""");

        var result = _projector.Project(message, "users");

        result.Should().NotBeNull();
        result!.ObjectType.Should().Be(ActivityObjectType.User);
        result.Verb.Should().Be(ActivityVerb.Registered);
        result.Visibility.Should().Be(ActivityVisibility.Public);
    }

    [Fact]
    public void Project_SubscriptionCreated_StaysPrivate()
    {
        var message = new EventMessage(
            MessageId: "msg-5",
            EventId: "evt-5",
            EventType: "SubscriptionCreatedEvent",
            OccurredAt: DateTime.UtcNow,
            Data: """{"id":"SUB-1","userId":"U0001"}""");

        var result = _projector.Project(message, "subscriptions");

        result.Should().NotBeNull();
        result!.Visibility.Should().Be(ActivityVisibility.Private);
    }

    [Fact]
    public void Project_UnknownEventType_ReturnsNull()
    {
        var message = new EventMessage(
            MessageId: "msg-6",
            EventId: "evt-6",
            EventType: "WeirdThingHappenedEvent",
            OccurredAt: DateTime.UtcNow,
            Data: "{}");

        var result = _projector.Project(message, "studies");

        result.Should().BeNull();
    }

    [Fact]
    public void Project_MissingPayloadFields_FallsBackToEventIdAsObjectId()
    {
        var message = new EventMessage(
            MessageId: "msg-7",
            EventId: "fallback-event-id",
            EventType: "StudyCreatedEvent",
            OccurredAt: DateTime.UtcNow,
            Data: "{}");

        var result = _projector.Project(message, "studies");

        result.Should().NotBeNull();
        result!.ObjectId.Should().Be("fallback-event-id");
        result.ActorUserId.Should().BeNull();
    }

    [Fact]
    public void Project_MalformedPayload_StillProjectsWithFallbackObjectId()
    {
        var message = new EventMessage(
            MessageId: "msg-8",
            EventId: "evt-8",
            EventType: "StudyCreatedEvent",
            OccurredAt: DateTime.UtcNow,
            Data: "not-json");

        var result = _projector.Project(message, "studies");

        result.Should().NotBeNull();
        result!.ObjectId.Should().Be("evt-8");
        result.PayloadJson.Should().Be("not-json");
    }

    [Fact]
    public void Project_PreservesOriginalOccurredAtAsUtc()
    {
        var occurredAt = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var message = new EventMessage(
            MessageId: "msg-9",
            EventId: "evt-9",
            EventType: "StudyCreatedEvent",
            OccurredAt: occurredAt,
            Data: """{"id":"S-9"}""");

        var result = _projector.Project(message, "studies");

        result.Should().NotBeNull();
        result!.OccurredAt.UtcDateTime.Should().Be(occurredAt);
    }
}
