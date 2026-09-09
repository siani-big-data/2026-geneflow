using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.Events;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines;

/// <summary>
/// Unit tests for the Pipeline aggregate root.
/// </summary>
public class PipelineTests
{
    private static PipelineId CreatePipelineId(long value = 1) => new(value);
    private static PipelineExecutionId CreateExecutionId(long value = 1) => new(value);
    private static StudyId CreateStudyId(long value = 1) => new(value);
    private static UserId CreateUserId(long value = 1) => new(value);
    private static TraceId CreateTraceId() => TraceId.New();
    private static PipelineName CreateName(string value = "Test Pipeline") => PipelineName.Create(value).Value;
    private static PipelineDescription CreateDescription(string? value = "Test description") =>
        PipelineDescription.Create(value).Value;
    private static StepConfiguration CreateConfig(string json = "{}") =>
        StepConfiguration.Create(json, StepType.Quality).Value;

    #region Create - Factory Method

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var pipelineId = CreatePipelineId();
        var studyId = CreateStudyId();
        var ownerId = CreateUserId();
        var name = CreateName("Quality Analysis Pipeline");
        var description = CreateDescription();

        // Act
        var result = Pipeline.Create(pipelineId, studyId, ownerId, name, description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(pipelineId);
        result.Value.StudyId.Should().Be(studyId);
        result.Value.OwnerId.Should().Be(ownerId);
        result.Value.Name.Should().Be(name);
        result.Value.Description.Should().Be(description);
    }

    [Fact]
    public void Create_ShouldSetStatusToDraft()
    {
        // Act
        var result = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), CreateUserId(),
            CreateName(), CreateDescription());

        // Assert
        result.Value.Status.Should().Be(PipelineStatus.Draft);
    }

    [Fact]
    public void Create_ShouldInitializeEmptyStepsList()
    {
        // Act
        var result = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), CreateUserId(),
            CreateName(), CreateDescription());

        // Assert
        result.Value.Steps.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldRaisePipelineCreatedEvent()
    {
        // Act
        var result = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), CreateUserId(),
            CreateName("My Pipeline"), CreateDescription());

        // Assert
        result.Value.DomainEvents.Should().ContainSingle();
        result.Value.DomainEvents.First().Should().BeOfType<PipelineCreatedEvent>();

        var evt = (PipelineCreatedEvent)result.Value.DomainEvents.First();
        evt.Name.Should().Be("My Pipeline");
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), CreateUserId(),
            CreateName(), CreateDescription());

        // Assert
        result.Value.CreatedAt.Should().BeOnOrAfter(beforeCreate);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_WhenDraft_ShouldUpdateFields()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName("Old Name"), CreateDescription("Old description")).Value;
        pipeline.ClearDomainEvents();

        var newName = CreateName("New Name");
        var newDescription = CreateDescription("New description");

        // Act
        var result = pipeline.Update(newName, newDescription, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Name.Should().Be(newName);
        pipeline.Description.Should().Be(newDescription);
    }

    [Fact]
    public void Update_WhenActive_ShouldReturnNotEditableError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipeline(ownerId);

        // Act
        var result = pipeline.Update(CreateName("New Name"), CreateDescription(), ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotEditable");
    }

    [Fact]
    public void Update_WhenArchived_ShouldReturnNotEditableError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.Archive(ownerId);

        // Act
        var result = pipeline.Update(CreateName("New Name"), CreateDescription(), ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotEditable");
    }

    [Fact]
    public void Update_ShouldSetModifiedAt()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var beforeUpdate = DateTime.UtcNow;

        // Act
        pipeline.Update(CreateName("New Name"), CreateDescription(), ownerId);

        // Assert
        pipeline.ModifiedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    [Fact]
    public void Update_AllFields_ShouldSucceed()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName("Original Name"), CreateDescription("Original description")).Value;
        pipeline.ClearDomainEvents();

        var newName = CreateName("Updated Pipeline Name");
        var newDescription = CreateDescription("Updated pipeline description with more details");

        // Act
        var result = pipeline.Update(newName, newDescription, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Name.Should().Be(newName);
        pipeline.Name.Value.Should().Be("Updated Pipeline Name");
        pipeline.Description.Should().Be(newDescription);
        pipeline.Description.Value.Should().Be("Updated pipeline description with more details");
    }

    #endregion

    #region Step Management - AddStep

    [Fact]
    public void AddStep_WhenDraft_ShouldAddStep()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.AddStep(StepType.Quality, CreateConfig(), "Quality Check", true, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Steps.Should().HaveCount(1);
        pipeline.Steps.First().StepType.Should().Be(StepType.Quality);
        pipeline.Steps.First().Label.Should().Be("Quality Check");
    }

    [Fact]
    public void AddStep_ShouldAssignCorrectOrder()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 2", true, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 3", true, ownerId);

        // Assert
        pipeline.Steps[0].Order.Should().Be(1);
        pipeline.Steps[1].Order.Should().Be(2);
        pipeline.Steps[2].Order.Should().Be(3);
    }

    [Fact]
    public void AddStep_WhenMaxStepsReached_ShouldReturnError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Add max steps
        for (int i = 0; i < Pipeline.MaxSteps; i++)
        {
            pipeline.AddStep(StepType.Quality, CreateConfig(), $"Step {i + 1}", true, ownerId);
        }

        // Act
        var result = pipeline.AddStep(StepType.Quality, CreateConfig(), "One Too Many", true, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("MaxStepsExceeded");
    }

    [Fact]
    public void AddStep_WhenActive_ShouldReturnNotEditableError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipeline(ownerId);

        // Act
        var result = pipeline.AddStep(StepType.Quality, CreateConfig(), "New Step", true, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotEditable");
    }

    [Fact]
    public void AddStep_WithLongLabel_ShouldReturnError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var longLabel = new string('a', 101);

        // Act
        var result = pipeline.AddStep(StepType.Quality, CreateConfig(), longLabel, true, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StepLabelTooLong");
    }

    [Fact]
    public void AddStep_ShouldRaisePipelineStepAddedEvent()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.ClearDomainEvents();

        // Act
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Quality Check", true, ownerId);

        // Assert
        pipeline.DomainEvents.Should().ContainSingle();
        pipeline.DomainEvents.First().Should().BeOfType<PipelineStepAddedEvent>();
    }

    [Fact]
    public void AddStep_WithNullLabel_ShouldSucceed()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act
        var result = pipeline.AddStep(StepType.Quality, CreateConfig(), null, true, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Label.Should().BeNull();
    }

    [Fact]
    public void AddStep_DisabledStep_ShouldSucceed()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act
        var result = pipeline.AddStep(StepType.Quality, CreateConfig(), "Disabled Step", false, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void AddStep_DuplicateStepType_ShouldSucceed()
    {
        // Arrange - Pipeline allows multiple steps of the same type
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act - Add two steps of the same type
        var result1 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Quality Step 1", true, ownerId);
        var result2 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Quality Step 2", true, ownerId);

        // Assert - Both should succeed (no duplicate validation exists)
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        pipeline.Steps.Should().HaveCount(2);
        pipeline.Steps.All(s => s.StepType == StepType.Quality).Should().BeTrue();
    }

    #endregion

    #region Step Management - UpdateStep

    [Fact]
    public void UpdateStep_WithValidData_ShouldUpdateStep()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var stepResult = pipeline.AddStep(StepType.Quality, CreateConfig(), "Old Label", true, ownerId);
        var stepId = stepResult.Value.Id;
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.UpdateStep(stepId, CreateConfig(), "New Label", false, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var step = pipeline.Steps.First(s => s.Id == stepId);
        step.Label.Should().Be("New Label");
        step.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void UpdateStep_NonExistentStep_ShouldReturnStepNotFoundError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act
        var result = pipeline.UpdateStep(Guid.NewGuid(), CreateConfig(), "Label", true, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StepNotFound");
    }

    [Fact]
    public void UpdateStep_WithLongLabel_ShouldReturnStepLabelTooLongError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var stepResult = pipeline.AddStep(StepType.Quality, CreateConfig(), "Original Label", true, ownerId);
        var stepId = stepResult.Value.Id;
        var longLabel = new string('a', 101);

        // Act
        var result = pipeline.UpdateStep(stepId, CreateConfig(), longLabel, true, ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StepLabelTooLong");
    }

    #endregion

    #region Step Management - RemoveStep

    [Fact]
    public void RemoveStep_ShouldRemoveAndReorderSteps()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId);
        var step2 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 2", true, ownerId).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 3", true, ownerId);
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.RemoveStep(step2.Id, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Steps.Should().HaveCount(2);
        pipeline.Steps[0].Order.Should().Be(1);
        pipeline.Steps[1].Order.Should().Be(2);
    }

    [Fact]
    public void RemoveStep_NonExistentStep_ShouldReturnStepNotFoundError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act
        var result = pipeline.RemoveStep(Guid.NewGuid(), ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StepNotFound");
    }

    [Fact]
    public void RemoveStep_ShouldRaisePipelineStepRemovedEvent()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var step = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step", true, ownerId).Value;
        pipeline.ClearDomainEvents();

        // Act
        pipeline.RemoveStep(step.Id, ownerId);

        // Assert
        pipeline.DomainEvents.Should().ContainSingle();
        pipeline.DomainEvents.First().Should().BeOfType<PipelineStepRemovedEvent>();
    }

    #endregion

    #region Step Management - ReorderSteps

    [Fact]
    public void ReorderSteps_WithValidOrder_ShouldReorderSteps()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var step1 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId).Value;
        var step2 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 2", true, ownerId).Value;
        var step3 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 3", true, ownerId).Value;

        // Act - Reverse the order
        var result = pipeline.ReorderSteps(new[] { step3.Id, step2.Id, step1.Id }.ToList(), ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Steps.First(s => s.Id == step3.Id).Order.Should().Be(1);
        pipeline.Steps.First(s => s.Id == step2.Id).Order.Should().Be(2);
        pipeline.Steps.First(s => s.Id == step1.Id).Order.Should().Be(3);
    }

    [Fact]
    public void ReorderSteps_WithInvalidStepCount_ShouldReturnError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var step1 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 2", true, ownerId);

        // Act - Only provide one step ID when there are two steps
        var result = pipeline.ReorderSteps(new[] { step1.Id }.ToList(), ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStepOrder");
    }

    [Fact]
    public void ReorderSteps_WithDuplicateIds_ShouldReturnError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var step1 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 2", true, ownerId);

        // Act - Duplicate ID
        var result = pipeline.ReorderSteps(new[] { step1.Id, step1.Id }.ToList(), ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("DuplicateStepOrder");
    }

    [Fact]
    public void ReorderSteps_WithUnknownStepId_ShouldReturnError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var step1 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 2", true, ownerId);

        // Act - Include unknown ID
        var result = pipeline.ReorderSteps(new[] { step1.Id, Guid.NewGuid() }.ToList(), ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StepNotFound");
    }

    [Fact]
    public void ReorderSteps_AfterStepRemoval_ShouldMaintainOrder()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        var step1 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId).Value;
        var step2 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 2", true, ownerId).Value;
        var step3 = pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 3", true, ownerId).Value;

        // Remove the middle step
        pipeline.RemoveStep(step2.Id, ownerId);

        // Act - Reorder the remaining steps (swap step1 and step3)
        var result = pipeline.ReorderSteps(new[] { step3.Id, step1.Id }.ToList(), ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Steps.Should().HaveCount(2);
        pipeline.Steps.First(s => s.Id == step3.Id).Order.Should().Be(1);
        pipeline.Steps.First(s => s.Id == step1.Id).Order.Should().Be(2);
    }

    #endregion

    #region Status Management - Activate

    [Fact]
    public void Activate_FromDraft_WithSteps_ShouldActivate()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId);
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.Activate(ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Status.Should().Be(PipelineStatus.Active);
    }

    [Fact]
    public void Activate_WithNoSteps_ShouldReturnNoStepsError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act
        var result = pipeline.Activate(ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoStepsConfigured");
    }

    [Fact]
    public void Activate_WithOnlyDisabledSteps_ShouldReturnNoStepsError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Disabled Step", false, ownerId);

        // Act
        var result = pipeline.Activate(ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoStepsConfigured");
    }

    [Fact]
    public void Activate_FromActive_ShouldReturnInvalidTransitionError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipeline(ownerId);

        // Act
        var result = pipeline.Activate(ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public void Activate_ShouldRaisePipelineActivatedEvent()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step", true, ownerId);
        pipeline.ClearDomainEvents();

        // Act
        pipeline.Activate(ownerId);

        // Assert
        pipeline.DomainEvents.Should().ContainSingle();
        pipeline.DomainEvents.First().Should().BeOfType<PipelineActivatedEvent>();
    }

    [Fact]
    public void Activate_WithNoEnabledSteps_ShouldFail()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        // Add only disabled steps
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Disabled Step 1", false, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Disabled Step 2", false, ownerId);

        // Act
        var result = pipeline.Activate(ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoStepsConfigured");
        pipeline.EnabledStepCount.Should().Be(0);
    }

    #endregion

    #region Status Management - Deactivate

    [Fact]
    public void Deactivate_FromActive_ShouldDeactivate()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipeline(ownerId);
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.Deactivate(ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Status.Should().Be(PipelineStatus.Draft);
    }

    [Fact]
    public void Deactivate_FromDraft_ShouldReturnInvalidTransitionError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act
        var result = pipeline.Deactivate(ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public void Deactivate_WhenNotActive_ShouldReturnError()
    {
        // Arrange - Pipeline is in Archived status
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.Archive(ownerId);

        // Act
        var result = pipeline.Deactivate(ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public void Deactivate_WhenAlreadyDraft_ShouldReturnError()
    {
        // Arrange - Pipeline is already in Draft status (initial state)
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.Status.Should().Be(PipelineStatus.Draft);

        // Act - Trying to deactivate when already in Draft
        var result = pipeline.Deactivate(ownerId);

        // Assert - Should fail because Draft -> Draft is not a valid transition
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    #endregion

    #region Status Management - Archive

    [Fact]
    public void Archive_FromDraft_ShouldArchive()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.Archive(ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Status.Should().Be(PipelineStatus.Archived);
    }

    [Fact]
    public void Archive_FromActive_ShouldArchive()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipeline(ownerId);
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.Archive(ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Status.Should().Be(PipelineStatus.Archived);
    }

    [Fact]
    public void Archive_ShouldRaisePipelineArchivedEvent()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.ClearDomainEvents();

        // Act
        pipeline.Archive(ownerId);

        // Assert
        pipeline.DomainEvents.Should().ContainSingle();
        pipeline.DomainEvents.First().Should().BeOfType<PipelineArchivedEvent>();
    }

    #endregion

    #region Status Management - Restore

    [Fact]
    public void RestoREDACTED()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.Archive(ownerId);
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.Restore(ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        pipeline.Status.Should().Be(PipelineStatus.Draft);
    }

    [Fact]
    public void RestoREDACTED()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Act
        var result = pipeline.Restore(ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    #endregion

    #region Execution

    [Fact]
    public void StartExecution_WhenActive_ShouldStartExecution()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipeline(ownerId);
        var executionId = CreateExecutionId();
        var traceId = CreateTraceId();
        pipeline.ClearDomainEvents();

        // Act
        var result = pipeline.StartExecution(executionId, traceId, ownerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.PipelineId.Should().Be(pipeline.Id);
        result.Value.TraceId.Should().Be(traceId);
    }

    [Fact]
    public void StartExecution_WhenNotActive_ShouldReturnNotExecutableError()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step", true, ownerId);

        // Act - Pipeline is still in Draft status
        var result = pipeline.StartExecution(CreateExecutionId(), CreateTraceId(), ownerId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotExecutable");
    }

    [Fact]
    public void StartExecution_ShouldRaisePipelineExecutionStartedEvent()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipeline(ownerId);
        pipeline.ClearDomainEvents();

        // Act
        pipeline.StartExecution(CreateExecutionId(), CreateTraceId(), ownerId);

        // Assert
        pipeline.DomainEvents.Should().ContainSingle();
        pipeline.DomainEvents.First().Should().BeOfType<PipelineExecutionStartedEvent>();
    }

    [Fact]
    public void StartExecution_ShouldIncludeEnabledStepsCount()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 1", true, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step 2", true, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Disabled", false, ownerId);
        pipeline.Activate(ownerId);
        pipeline.ClearDomainEvents();

        // Act
        pipeline.StartExecution(CreateExecutionId(), CreateTraceId(), ownerId);

        // Assert
        var evt = (PipelineExecutionStartedEvent)pipeline.DomainEvents.First();
        evt.TotalSteps.Should().Be(2); // Only enabled steps
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void CanBeEdited_WhenDraft_ShouldReturnTrue()
    {
        // Arrange
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), CreateUserId(),
            CreateName(), CreateDescription()).Value;

        // Assert
        pipeline.CanBeEdited.Should().BeTrue();
    }

    [Fact]
    public void CanBeEdited_WhenActive_ShouldReturnFalse()
    {
        // Arrange
        var pipeline = CreateActivePipeline(CreateUserId());

        // Assert
        pipeline.CanBeEdited.Should().BeFalse();
    }

    [Fact]
    public void CanBeExecuted_WhenActive_ShouldReturnTrue()
    {
        // Arrange
        var pipeline = CreateActivePipeline(CreateUserId());

        // Assert
        pipeline.CanBeExecuted.Should().BeTrue();
    }

    [Fact]
    public void CanBeExecuted_WhenDraft_ShouldReturnFalse()
    {
        // Arrange
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), CreateUserId(),
            CreateName(), CreateDescription()).Value;

        // Assert
        pipeline.CanBeExecuted.Should().BeFalse();
    }

    [Fact]
    public void CanBeDeleted_WhenNotArchived_ShouldReturnTrue()
    {
        // Arrange
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), CreateUserId(),
            CreateName(), CreateDescription()).Value;

        // Assert
        pipeline.CanBeDeleted.Should().BeTrue();
    }

    [Fact]
    public void CanBeDeleted_WhenArchived_ShouldReturnFalse()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.Archive(ownerId);

        // Assert
        pipeline.CanBeDeleted.Should().BeFalse();
    }

    [Fact]
    public void EnabledStepCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Enabled 1", true, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Enabled 2", true, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Disabled", false, ownerId);

        // Assert
        pipeline.EnabledStepCount.Should().Be(2);
    }

    [Fact]
    public void EnabledStepCount_WhenNoSteps_ShouldReturnZero()
    {
        // Arrange
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), CreateUserId(),
            CreateName(), CreateDescription()).Value;

        // Assert
        pipeline.EnabledStepCount.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private Pipeline CreateActivePipeline(UserId ownerId)
    {
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step", true, ownerId);
        pipeline.Activate(ownerId);
        return pipeline;
    }

    #endregion
}
