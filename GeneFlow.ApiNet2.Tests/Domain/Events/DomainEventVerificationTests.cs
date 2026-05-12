using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.Events;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.Events;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Events;

/// <summary>
/// Comprehensive domain event verification tests.
/// Verifies that domain events are raised with correct payloads for all major entity actions.
/// </summary>
public class DomainEventVerificationTests
{
    #region Helper Methods

    private static StudyId CreateStudyId(long value = 1) => new(value);
    private static UserId CreateUserId(long value = 1) => new(value);
    private static TraceId CreateTraceId() => TraceId.New();
    private static TraceId CreateTraceId(Guid value) => TraceId.From(value);
    private static PipelineId CreatePipelineId(long value = 1) => new(value);

    private static StudyTitle CreateStudyTitle(string value = "Test Study") =>
        StudyTitle.Create(value).Value;

    private static StudyDescription CreateStudyDescription(string value = "Test description") =>
        StudyDescription.Create(value).Value;

    private static TraceName CreateTraceName(string value = "Test Trace") =>
        TraceName.Create(value).Value;

    private static TraceDescription CreateTraceDescription(string? value = "Test trace description") =>
        TraceDescription.Create(value).Value;

    private static TraceFile CreateTraceFile() =>
        TraceFile.Create(
            "test_file.ab1",
            "application/octet-stream",
            "/storage/test_file.ab1",
            1024,
            "abc123def456").Value;

    private static PipelineName CreatePipelineName(string value = "Test Pipeline") =>
        PipelineName.Create(value).Value;

    private static PipelineDescription CreatePipelineDescription(string? value = "Test pipeline description") =>
        PipelineDescription.Create(value).Value;

    private static Email CreateEmail(string value = "test@example.com") =>
        Email.Create(value).Value;

    private static Username CreateUsername(string value = "testuser") =>
        Username.Create(value).Value;

    private static PasswordHash CreatePasswordHash() =>
        PasswordHash.Create("$2a$12$validhashvaluehere1234567890abcdef").Value;

    private static QualityMetrics CreateQualityMetrics(int totalBases = 500) =>
        QualityMetrics.Create(
            averageQualityScore: 35.5m,
            totalBases: totalBases,
            qualityAboveQ20Percentage: 95.0m,
            qualityAboveQ30Percentage: 85.0m,
            trimmedLength: totalBases,
            gcContentPercentage: 52.0m).Value;

    private static StepConfiguration CreateStepConfiguration(StepType stepType) =>
        StepConfiguration.Create("{}", stepType).Value;

    private Study CreateStudy(UserId? ownerId = null)
    {
        return Study.Create(
            CreateStudyId(),
            ownerId ?? CreateUserId(),
            CreateStudyTitle(),
            CreateStudyDescription(),
            ResearchField.Genomics).Value;
    }

    private Trace CreateTrace(StudyId? studyId = null, UserId? uploadedBy = null)
    {
        return Trace.Create(
            CreateTraceId(),
            studyId ?? CreateStudyId(),
            uploadedBy ?? CreateUserId(),
            CreateTraceName(),
            CreateTraceDescription(),
            CreateTraceFile(),
            TraceFormat.AB1).Value;
    }

    private Trace CreateProcessedTrace(StudyId? studyId = null, UserId? uploadedBy = null)
    {
        var trace = CreateTrace(studyId, uploadedBy);
        trace.StartProcessing();
        trace.TransitionToProcessing();
        trace.CompleteProcessing(CreateQualityMetrics(), hasChromatogramData: true);
        trace.ClearDomainEvents(); // Clear events from setup
        return trace;
    }

    private Pipeline CreatePipeline(StudyId? studyId = null, UserId? ownerId = null)
    {
        return Pipeline.Create(
            CreatePipelineId(),
            studyId ?? CreateStudyId(),
            ownerId ?? CreateUserId(),
            CreatePipelineName(),
            CreatePipelineDescription()).Value;
    }

    private User CreateUser(UserId? id = null)
    {
        return User.Create(
            id ?? CreateUserId(),
            CreateEmail(),
            CreateUsername(),
            CreatePasswordHash()).Value;
    }

    #endregion

    #region Study Events

    [Fact]
    public void Study_WhenCreated_ShouldRaiseStudyCreatedEvent()
    {
        // Arrange
        var studyId = CreateStudyId(42);
        var ownerId = CreateUserId(1);
        var title = CreateStudyTitle("My Research Study");

        // Act
        var study = Study.Create(
            studyId,
            ownerId,
            title,
            CreateStudyDescription(),
            ResearchField.Genomics).Value;

        // Assert
        study.DomainEvents.Should().ContainSingle();
        var domainEvent = study.DomainEvents.First();
        domainEvent.Should().BeOfType<StudyCreatedEvent>();

        var studyCreatedEvent = (StudyCreatedEvent)domainEvent;
        studyCreatedEvent.StudyId.Should().Be(studyId);
        studyCreatedEvent.Title.Should().Be("My Research Study");
        studyCreatedEvent.OwnerId.Should().Be(ownerId);
        studyCreatedEvent.ResearchField.Should().Be(ResearchField.Genomics);
    }

    [Fact]
    public void Study_WhenStatusChanged_ShouldRaiseStudyStatusChangedEvent()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var study = CreateStudy(ownerId);
        study.ClearDomainEvents();

        // Act
        var result = study.ChangeStatus(StudyStatus.Active, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle();

        var domainEvent = study.DomainEvents.First();
        domainEvent.Should().BeOfType<StudyStatusChangedEvent>();

        var statusChangedEvent = (StudyStatusChangedEvent)domainEvent;
        statusChangedEvent.StudyId.Should().Be(study.Id);
        statusChangedEvent.OldStatus.Should().Be(StudyStatus.Draft);
        statusChangedEvent.NewStatus.Should().Be(StudyStatus.Active);
        statusChangedEvent.ChangedBy.Should().Be(ownerId);
    }

    [Fact]
    public void Study_WhenMemberAdded_ShouldRaiseStudyMemberAddedEvent()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var newMemberId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.ClearDomainEvents();

        // Act
        var result = study.AddMember(newMemberId, StudyRole.Editor, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle();

        var domainEvent = study.DomainEvents.First();
        domainEvent.Should().BeOfType<StudyMemberAddedEvent>();

        var memberAddedEvent = (StudyMemberAddedEvent)domainEvent;
        memberAddedEvent.StudyId.Should().Be(study.Id);
        memberAddedEvent.MemberUserId.Should().Be(newMemberId);
        memberAddedEvent.Role.Should().Be(StudyRole.Editor);
        memberAddedEvent.AddedBy.Should().Be(ownerId);
    }

    [Fact]
    public void Study_WhenMemberRemoved_ShouldRaiseStudyMemberRemovedEvent()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var memberId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);
        study.ClearDomainEvents();

        // Act
        var result = study.RemoveMember(memberId, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle();

        var domainEvent = study.DomainEvents.First();
        domainEvent.Should().BeOfType<StudyMemberRemovedEvent>();

        var memberRemovedEvent = (StudyMemberRemovedEvent)domainEvent;
        memberRemovedEvent.StudyId.Should().Be(study.Id);
        memberRemovedEvent.MemberUserId.Should().Be(memberId);
        memberRemovedEvent.RemovedBy.Should().Be(ownerId);
    }

    [Fact]
    public void Study_WhenMemberRoleChanged_ShouldRaiseStudyMemberRoleChangedEvent()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var memberId = CreateUserId(2);
        var study = CreateStudy(ownerId);
        study.AddMember(memberId, StudyRole.Viewer, ownerId);
        study.ClearDomainEvents();

        // Act
        var result = study.ChangeMemberRole(memberId, StudyRole.Admin, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle();

        var domainEvent = study.DomainEvents.First();
        domainEvent.Should().BeOfType<StudyMemberRoleChangedEvent>();

        var roleChangedEvent = (StudyMemberRoleChangedEvent)domainEvent;
        roleChangedEvent.StudyId.Should().Be(study.Id);
        roleChangedEvent.MemberUserId.Should().Be(memberId);
        roleChangedEvent.OldRole.Should().Be(StudyRole.Viewer);
        roleChangedEvent.NewRole.Should().Be(StudyRole.Admin);
        roleChangedEvent.ChangedBy.Should().Be(ownerId);
    }

    [Fact]
    public void Study_WhenOwnershipTransferred_ShouldRaiseOwnershipTransferredEvent()
    {
        // Arrange
        var originalOwnerId = CreateUserId(1);
        var newOwnerId = CreateUserId(2);
        var study = CreateStudy(originalOwnerId);
        study.AddMember(newOwnerId, StudyRole.Admin, originalOwnerId);
        study.ClearDomainEvents();

        // Act
        var result = study.TransferOwnership(newOwnerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle();

        var domainEvent = study.DomainEvents.First();
        domainEvent.Should().BeOfType<StudyOwnershipTransferredEvent>();

        var transferEvent = (StudyOwnershipTransferredEvent)domainEvent;
        transferEvent.StudyId.Should().Be(study.Id);
        transferEvent.PreviousOwnerId.Should().Be(originalOwnerId);
        transferEvent.NewOwnerId.Should().Be(newOwnerId);
    }

    #endregion

    #region Trace Events

    [Fact]
    public void Trace_WhenCreated_ShouldRaiseTraceUploadedEvent()
    {
        // Arrange
        var traceId = CreateTraceId();
        var studyId = CreateStudyId(1);
        var uploadedBy = CreateUserId(1);

        // Act
        var trace = Trace.Create(
            traceId,
            studyId,
            uploadedBy,
            CreateTraceName(),
            CreateTraceDescription(),
            CreateTraceFile(),
            TraceFormat.AB1).Value;

        // Assert
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<TraceUploadedEvent>();

        var uploadedEvent = (TraceUploadedEvent)domainEvent;
        uploadedEvent.TraceId.Should().Be(traceId);
        uploadedEvent.StudyId.Should().Be(studyId);
        uploadedEvent.FileName.Should().Be("test_file.ab1");
        uploadedEvent.Format.Should().Be(TraceFormat.AB1);
        uploadedEvent.UploadedBy.Should().Be(uploadedBy);
    }

    [Fact]
    public void Trace_WhenProcessingStarted_ShouldRaiseProcessingStartedEvent()
    {
        // Arrange
        var trace = CreateTrace();
        trace.ClearDomainEvents();

        // Act
        var result = trace.StartProcessing();

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<TraceProcessingStartedEvent>();

        var processingStartedEvent = (TraceProcessingStartedEvent)domainEvent;
        processingStartedEvent.TraceId.Should().Be(trace.Id);
        processingStartedEvent.StudyId.Should().Be(trace.StudyId);
    }

    [Fact]
    public void Trace_WhenProcessingCompleted_ShouldRaiseProcessingCompletedEvent()
    {
        // Arrange
        var trace = CreateTrace();
        trace.StartProcessing();
        trace.TransitionToProcessing();
        trace.ClearDomainEvents();

        var qualityMetrics = CreateQualityMetrics();

        // Act
        var result = trace.CompleteProcessing(qualityMetrics, hasChromatogramData: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<TraceProcessedEvent>();

        var processedEvent = (TraceProcessedEvent)domainEvent;
        processedEvent.TraceId.Should().Be(trace.Id);
        processedEvent.StudyId.Should().Be(trace.StudyId);
        processedEvent.AverageQualityScore.Should().Be(35.5m);
    }

    [Fact]
    public void Trace_WhenProcessingFailed_ShouldRaiseProcessingFailedEvent()
    {
        // Arrange
        var trace = CreateTrace();
        trace.StartProcessing();
        trace.TransitionToProcessing();
        trace.ClearDomainEvents();

        var failureReason = "Invalid file format detected";

        // Act
        var result = trace.FailProcessing(failureReason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<TraceProcessingFailedEvent>();

        var failedEvent = (TraceProcessingFailedEvent)domainEvent;
        failedEvent.TraceId.Should().Be(trace.Id);
        failedEvent.StudyId.Should().Be(trace.StudyId);
        failedEvent.Reason.Should().Be(failureReason);
    }

    [Fact]
    public void Trace_WhenTrimApplied_ShouldRaiseTrimAppliedEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var trace = CreateProcessedTrace(uploadedBy: userId);

        // Act
        var result = trace.AddTrim(
            TrimType.Manual,
            startPosition: 0,
            endPosition: 50,
            TrimEnd.FivePrime,
            algorithm: "manual",
            appliedBy: userId,
            reason: "Low quality region");

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<TraceTrimAppliedEvent>();

        var trimAppliedEvent = (TraceTrimAppliedEvent)domainEvent;
        trimAppliedEvent.TraceId.Should().Be(trace.Id);
        trimAppliedEvent.StudyId.Should().Be(trace.StudyId);
        trimAppliedEvent.StartPosition.Should().Be(0);
        trimAppliedEvent.EndPosition.Should().Be(50);
        trimAppliedEvent.TrimEnd.Should().Be(TrimEnd.FivePrime);
        trimAppliedEvent.Algorithm.Should().Be("manual");
        trimAppliedEvent.AppliedBy.Should().Be(userId);
    }

    [Fact]
    public void Trace_WhenTrimUndone_ShouldRaiseTrimUndoneEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var trace = CreateProcessedTrace(uploadedBy: userId);
        var trimResult = trace.AddTrim(
            TrimType.Manual,
            startPosition: 0,
            endPosition: 50,
            TrimEnd.FivePrime,
            algorithm: "manual",
            appliedBy: userId);
        var trimId = trimResult.Value.Id;
        trace.ClearDomainEvents();

        // Act
        var result = trace.UndoTrim(trimId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<TraceTrimUndoneEvent>();

        var trimUndoneEvent = (TraceTrimUndoneEvent)domainEvent;
        trimUndoneEvent.TraceId.Should().Be(trace.Id);
        trimUndoneEvent.StudyId.Should().Be(trace.StudyId);
        trimUndoneEvent.TrimId.Should().Be(trimId);
        trimUndoneEvent.UndoneBy.Should().Be(userId);
    }

    [Fact]
    public void Trace_WhenAnnotationCreated_ShouldRaiseAnnotationCreatedEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var trace = CreateProcessedTrace(uploadedBy: userId);

        // Act
        var result = trace.AddAnnotation(
            type: AnnotationType.Region,
            label: "Promoter Region",
            description: "Key regulatory element",
            startPosition: 10,
            endPosition: 50,
            strand: AnnotationStrand.Plus,
            color: "#FF5733",
            isShared: true,
            metadata: null,
            createdBy: userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<AnnotationCreatedEvent>();

        var annotationCreatedEvent = (AnnotationCreatedEvent)domainEvent;
        annotationCreatedEvent.TraceId.Should().Be(trace.Id);
        annotationCreatedEvent.StudyId.Should().Be(trace.StudyId);
        annotationCreatedEvent.Type.Should().Be(AnnotationType.Region);
        annotationCreatedEvent.Label.Should().Be("Promoter Region");
        annotationCreatedEvent.StartPosition.Should().Be(10);
        annotationCreatedEvent.EndPosition.Should().Be(50);
        annotationCreatedEvent.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public void Trace_WhenAnnotationUpdated_ShouldRaiseAnnotationUpdatedEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var trace = CreateProcessedTrace(uploadedBy: userId);
        var annotationResult = trace.AddAnnotation(
            type: AnnotationType.Region,
            label: "Initial Label",
            description: null,
            startPosition: 10,
            endPosition: 50,
            strand: AnnotationStrand.Plus,
            color: "#FF5733",
            isShared: false,
            metadata: null,
            createdBy: userId);
        var annotationId = annotationResult.Value.Id;
        trace.ClearDomainEvents();

        // Act
        var result = trace.UpdateAnnotation(
            annotationId: annotationId,
            label: "Updated Label",
            description: "Updated description",
            startPosition: 15,
            endPosition: 55,
            strand: AnnotationStrand.Minus,
            color: "#00FF00",
            isShared: true,
            metadata: null,
            updatedBy: userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<AnnotationUpdatedEvent>();

        var annotationUpdatedEvent = (AnnotationUpdatedEvent)domainEvent;
        annotationUpdatedEvent.AnnotationId.Should().Be(annotationId);
        annotationUpdatedEvent.TraceId.Should().Be(trace.Id);
        annotationUpdatedEvent.StudyId.Should().Be(trace.StudyId);
        annotationUpdatedEvent.UpdatedBy.Should().Be(userId);
    }

    [Fact]
    public void Trace_WhenAnnotationDeleted_ShouldRaiseAnnotationDeletedEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var trace = CreateProcessedTrace(uploadedBy: userId);
        var annotationResult = trace.AddAnnotation(
            type: AnnotationType.Feature,
            label: "To Be Deleted",
            description: null,
            startPosition: 10,
            endPosition: 50,
            strand: AnnotationStrand.None,
            color: "#FF0000",
            isShared: false,
            metadata: null,
            createdBy: userId);
        var annotationId = annotationResult.Value.Id;
        trace.ClearDomainEvents();

        // Act
        var result = trace.RemoveAnnotation(annotationId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<AnnotationDeletedEvent>();

        var annotationDeletedEvent = (AnnotationDeletedEvent)domainEvent;
        annotationDeletedEvent.AnnotationId.Should().Be(annotationId);
        annotationDeletedEvent.TraceId.Should().Be(trace.Id);
        annotationDeletedEvent.StudyId.Should().Be(trace.StudyId);
        annotationDeletedEvent.DeletedBy.Should().Be(userId);
    }

    [Fact]
    public void Trace_WhenSequenceEditCreated_ShouldRaiseEditCreatedEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var trace = CreateProcessedTrace(uploadedBy: userId);

        // Act
        var result = trace.AddEdit(
            editType: EditType.Change,
            position: 100,
            originalBase: 'A',
            newBase: 'T',
            reason: "Correcting sequencing error",
            editedBy: userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<SequenceEditCreatedEvent>();

        var editCreatedEvent = (SequenceEditCreatedEvent)domainEvent;
        editCreatedEvent.TraceId.Should().Be(trace.Id);
        editCreatedEvent.StudyId.Should().Be(trace.StudyId);
        editCreatedEvent.EditType.Should().Be(EditType.Change);
        editCreatedEvent.Position.Should().Be(100);
        editCreatedEvent.EditedBy.Should().Be(userId);
    }

    [Fact]
    public void Trace_WhenSequenceEditUndone_ShouldRaiseEditUndoneEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var trace = CreateProcessedTrace(uploadedBy: userId);
        var editResult = trace.AddEdit(
            editType: EditType.Insert,
            position: 50,
            originalBase: null,
            newBase: 'G',
            reason: "Adding missing base",
            editedBy: userId);
        var editId = editResult.Value.Id;
        trace.ClearDomainEvents();

        // Act
        var result = trace.UndoEdit(editId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle();

        var domainEvent = trace.DomainEvents.First();
        domainEvent.Should().BeOfType<SequenceEditUndoneEvent>();

        var editUndoneEvent = (SequenceEditUndoneEvent)domainEvent;
        editUndoneEvent.TraceId.Should().Be(trace.Id);
        editUndoneEvent.StudyId.Should().Be(trace.StudyId);
        editUndoneEvent.EditId.Should().Be(editId);
        editUndoneEvent.UndoneBy.Should().Be(userId);
    }

    #endregion

    #region Pipeline Events

    [Fact]
    public void Pipeline_WhenCreated_ShouldRaisePipelineCreatedEvent()
    {
        // Arrange
        var pipelineId = CreatePipelineId(42);
        var studyId = CreateStudyId(1);
        var ownerId = CreateUserId(1);

        // Act
        var pipeline = Pipeline.Create(
            pipelineId,
            studyId,
            ownerId,
            CreatePipelineName("My Analysis Pipeline"),
            CreatePipelineDescription()).Value;

        // Assert
        pipeline.DomainEvents.Should().ContainSingle();

        var domainEvent = pipeline.DomainEvents.First();
        domainEvent.Should().BeOfType<PipelineCreatedEvent>();

        var createdEvent = (PipelineCreatedEvent)domainEvent;
        createdEvent.PipelineId.Should().Be(pipelineId);
        createdEvent.StudyId.Should().Be(studyId);
        createdEvent.Name.Should().Be("My Analysis Pipeline");
        createdEvent.CreatedBy.Should().Be(ownerId);
    }

    [Fact]
    public void Pipeline_WhenActivated_ShouldRaisePipelineActivatedEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var pipeline = CreatePipeline(ownerId: userId);
        // Add a step so pipeline can be activated
        pipeline.AddStep(StepType.Quality, CreateStepConfiguration(StepType.Quality), null, true, userId);
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.Activate(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.DomainEvents.Should().ContainSingle();

        var domainEvent = pipeline.DomainEvents.First();
        domainEvent.Should().BeOfType<PipelineActivatedEvent>();

        var activatedEvent = (PipelineActivatedEvent)domainEvent;
        activatedEvent.PipelineId.Should().Be(pipeline.Id);
        activatedEvent.ActivatedBy.Should().Be(userId);
    }

    [Fact]
    public void Pipeline_WhenDeactivated_ShouldNotRaiseDomainEvent()
    {
        // Note: Based on code review, Deactivate does not raise a domain event.
        // This test documents expected behavior.

        // Arrange
        var userId = CreateUserId(1);
        var pipeline = CreatePipeline(ownerId: userId);
        pipeline.AddStep(StepType.Quality, CreateStepConfiguration(StepType.Quality), null, true, userId);
        pipeline.Activate(userId);
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.Deactivate(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Deactivate does not raise domain events in current implementation
        pipeline.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Pipeline_WhenStepAdded_ShouldRaisePipelineStepAddedEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var pipeline = CreatePipeline(ownerId: userId);
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.AddStep(
            StepType.Trimming,
            CreateStepConfiguration(StepType.Trimming),
            label: "Quality Trimming Step",
            isEnabled: true,
            addedBy: userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.DomainEvents.Should().ContainSingle();

        var domainEvent = pipeline.DomainEvents.First();
        domainEvent.Should().BeOfType<PipelineStepAddedEvent>();

        var stepAddedEvent = (PipelineStepAddedEvent)domainEvent;
        stepAddedEvent.PipelineId.Should().Be(pipeline.Id);
        stepAddedEvent.StepType.Should().Be(StepType.Trimming);
        stepAddedEvent.Order.Should().Be(1);
    }

    [Fact]
    public void Pipeline_WhenStepRemoved_ShouldRaisePipelineStepRemovedEvent()
    {
        // Arrange
        var userId = CreateUserId(1);
        var pipeline = CreatePipeline(ownerId: userId);
        var stepResult = pipeline.AddStep(
            StepType.Quality,
            CreateStepConfiguration(StepType.Quality),
            null,
            true,
            userId);
        var stepId = stepResult.Value.Id;
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.RemoveStep(stepId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.DomainEvents.Should().ContainSingle();

        var domainEvent = pipeline.DomainEvents.First();
        domainEvent.Should().BeOfType<PipelineStepRemovedEvent>();

        var stepRemovedEvent = (PipelineStepRemovedEvent)domainEvent;
        stepRemovedEvent.PipelineId.Should().Be(pipeline.Id);
        stepRemovedEvent.StepId.Should().Be(stepId);
        stepRemovedEvent.StepType.Should().Be(StepType.Quality);
        stepRemovedEvent.Order.Should().Be(1);
    }

    #endregion

    #region User Events

    [Fact]
    public void User_WhenRegistered_ShouldRaiseUserRegisteredEvent()
    {
        // Arrange
        var userId = CreateUserId(42);
        var email = CreateEmail("newuser@example.com");
        var username = CreateUsername("newuser");

        // Act
        var user = User.Create(userId, email, username, CreatePasswordHash()).Value;

        // Assert
        user.DomainEvents.Should().ContainSingle();

        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserRegisteredEvent>();

        var registeredEvent = (UserRegisteredEvent)domainEvent;
        registeredEvent.UserId.Should().Be(userId);
        registeredEvent.Email.Should().Be("newuser@example.com");
        registeredEvent.Username.Should().Be("newuser");
        registeredEvent.EmailVerificationToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void User_WhenEmailVerified_ShouldRaiseUserEmailVerifiedEvent()
    {
        // Arrange
        var user = CreateUser();
        var verificationToken = user.EmailVerificationToken!;
        user.ClearDomainEvents();

        // Act
        var result = user.VerifyEmail(verificationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle();

        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserEmailVerifiedEvent>();

        var emailVerifiedEvent = (UserEmailVerifiedEvent)domainEvent;
        emailVerifiedEvent.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void User_WhenTwoFactorEnabled_ShouldRaiseTwoFactorEnabledEvent()
    {
        // Arrange
        var user = CreateUser();
        user.ClearDomainEvents();

        // Act
        var result = user.EnableTwoFactor();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle();

        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserTwoFactorEnabledEvent>();

        var twoFactorEnabledEvent = (UserTwoFactorEnabledEvent)domainEvent;
        twoFactorEnabledEvent.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void User_WhenTwoFactorDisabled_ShouldRaiseTwoFactorDisabledEvent()
    {
        // Arrange
        var user = CreateUser();
        user.EnableTwoFactor();
        user.ClearDomainEvents();

        // Act
        var result = user.DisableTwoFactor();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle();

        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserTwoFactorDisabledEvent>();

        var twoFactorDisabledEvent = (UserTwoFactorDisabledEvent)domainEvent;
        twoFactorDisabledEvent.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void User_WhenLockedOut_ShouldRaiseUserLockedOutEvent()
    {
        // Arrange
        var user = CreateUser();
        user.ClearDomainEvents();

        // Act - Record failed logins until lockout (typically 5 failures)
        for (int i = 0; i < 5; i++)
        {
            user.RecordFailedLogin();
        }

        // Assert
        user.IsLockedOut.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle();

        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserLockedOutEvent>();

        var lockedOutEvent = (UserLockedOutEvent)domainEvent;
        lockedOutEvent.UserId.Should().Be(user.Id);
        lockedOutEvent.LockoutEnd.Should().BeAfter(DateTime.UtcNow);
        lockedOutEvent.FailedAttempts.Should().Be(5);
    }

    #endregion

    #region Event Payload Consistency Tests

    [Fact]
    public void DomainEvents_ShouldHaveUniqueEventIds()
    {
        // Arrange
        var study = CreateStudy();
        study.ClearDomainEvents();

        // Act - Perform multiple actions
        study.AddMember(CreateUserId(2), StudyRole.Editor, CreateUserId(1));
        study.AddMember(CreateUserId(3), StudyRole.Viewer, CreateUserId(1));

        // Assert
        study.DomainEvents.Should().HaveCount(2);

        var eventIds = study.DomainEvents.Select(e => e.EventId).ToList();
        eventIds.Distinct().Should().HaveCount(2, "each domain event should have a unique EventId");
    }

    [Fact]
    public void DomainEvents_ShouldHaveOccurredAtSet()
    {
        // Arrange
        var beforeAction = DateTime.UtcNow;

        // Act
        var study = CreateStudy();

        // Assert
        var domainEvent = study.DomainEvents.First();
        domainEvent.OccurredAt.Should().BeOnOrAfter(beforeAction);
        domainEvent.OccurredAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var study = CreateStudy();
        study.DomainEvents.Should().NotBeEmpty();

        // Act
        study.ClearDomainEvents();

        // Assert
        study.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MultipleActions_ShouldAccumulateEvents()
    {
        // Arrange
        var ownerId = CreateUserId(1);
        var study = CreateStudy(ownerId);
        study.ClearDomainEvents();

        // Act
        study.AddMember(CreateUserId(2), StudyRole.Editor, ownerId);
        study.AddMember(CreateUserId(3), StudyRole.Viewer, ownerId);
        study.ChangeStatus(StudyStatus.Active, ownerId);

        // Assert
        study.DomainEvents.Should().HaveCount(3);
        study.DomainEvents[0].Should().BeOfType<StudyMemberAddedEvent>();
        study.DomainEvents[1].Should().BeOfType<StudyMemberAddedEvent>();
        study.DomainEvents[2].Should().BeOfType<StudyStatusChangedEvent>();
    }

    #endregion
}
