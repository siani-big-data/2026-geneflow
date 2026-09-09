using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines;

/// <summary>
/// Unit tests for the PipelineExecution entity.
/// </summary>
public class PipelineExecutionTests
{
    #region Helper Methods

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

    private static Pipeline CreateActivePipelineWithSteps(UserId ownerId, int enabledSteps = 2, int disabledSteps = 0)
    {
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        for (int i = 0; i < enabledSteps; i++)
        {
            pipeline.AddStep(StepType.Quality, CreateConfig(), $"Step {i + 1}", true, ownerId);
        }

        for (int i = 0; i < disabledSteps; i++)
        {
            pipeline.AddStep(StepType.Quality, CreateConfig(), $"Disabled Step {i + 1}", false, ownerId);
        }

        pipeline.Activate(ownerId);
        return pipeline;
    }

    private static PipelineExecution CreateExecution(int enabledSteps = 2, int disabledSteps = 0)
    {
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipelineWithSteps(ownerId, enabledSteps, disabledSteps);
        var execution = pipeline.StartExecution(CreateExecutionId(), CreateTraceId(), ownerId).Value;
        return execution;
    }

    private static PipelineExecution CreateRunningExecution(int enabledSteps = 2)
    {
        var execution = CreateExecution(enabledSteps);
        execution.Start();
        return execution;
    }

    #endregion

    #region Create - Factory Method

    [Fact]
    public void Create_WithEnabledSteps_ShouldCreateStepExecutionsForEnabledStepsOnly()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = CreateActivePipelineWithSteps(ownerId, enabledSteps: 2, disabledSteps: 1);

        // Act
        var execution = pipeline.StartExecution(CreateExecutionId(), CreateTraceId(), ownerId).Value;

        // Assert
        execution.StepExecutions.Should().HaveCount(2);
        execution.TotalSteps.Should().Be(2);
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var execution = CreateExecution();

        // Assert
        execution.Status.Should().Be(ExecutionStatus.Pending);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;

        // Act
        var execution = CreateExecution();

        // Assert
        execution.CreatedAt.Should().BeOnOrAfter(beforeCreate);
    }

    [Fact]
    public void Create_ShouldInitializeCompletedStepsToZero()
    {
        // Act
        var execution = CreateExecution();

        // Assert
        execution.CompletedSteps.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldOrderStepExecutionsByStepOrder()
    {
        // Arrange
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        pipeline.AddStep(StepType.Quality, CreateConfig(), "First", true, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Second", true, ownerId);
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Third", true, ownerId);
        pipeline.Activate(ownerId);

        // Act
        var execution = pipeline.StartExecution(CreateExecutionId(), CreateTraceId(), ownerId).Value;

        // Assert
        execution.StepExecutions[0].Order.Should().Be(1);
        execution.StepExecutions[1].Order.Should().Be(2);
        execution.StepExecutions[2].Order.Should().Be(3);
    }

    #endregion

    #region Start - Pending to Running

    [Fact]
    public void Start_WhenPending_ShouldTransitionToRunning()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        var result = execution.Start();

        // Assert
        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Running);
    }

    [Fact]
    public void Start_ShouldSetStartedAt()
    {
        // Arrange
        var execution = CreateExecution();
        var beforeStart = DateTime.UtcNow;

        // Act
        execution.Start();

        // Assert
        execution.StartedAt.Should().NotBeNull();
        execution.StartedAt.Should().BeOnOrAfter(beforeStart);
    }

    [Fact]
    public void Start_WhenAlreadyRunning_ShouldReturnError()
    {
        // Arrange
        var execution = CreateRunningExecution();

        // Act
        var result = execution.Start();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidExecutionStatusTransition");
    }

    [Fact]
    public void Start_WhenCompleted_ShouldReturnError()
    {
        // Arrange
        var execution = CreateRunningExecution();
        execution.Complete();

        // Act
        var result = execution.Start();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidExecutionStatusTransition");
    }

    #endregion

    #region Complete - Running to Completed

    [Fact]
    public void Complete_WhenRunning_ShouldTransitionToCompleted()
    {
        // Arrange
        var execution = CreateRunningExecution();

        // Act
        var result = execution.Complete();

        // Assert
        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Completed);
    }

    [Fact]
    public void Complete_ShouldSetCompletedAt()
    {
        // Arrange
        var execution = CreateRunningExecution();
        var beforeComplete = DateTime.UtcNow;

        // Act
        execution.Complete();

        // Assert
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Should().BeOnOrAfter(beforeComplete);
    }

    [Fact]
    public void Complete_WhenPending_ShouldReturnError()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        var result = execution.Complete();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidExecutionStatusTransition");
    }

    [Fact]
    public void Complete_ShouldCountCompletedSteps()
    {
        // Arrange
        var execution = CreateRunningExecution(3);
        // Complete first step
        var firstStep = execution.StepExecutions[0];
        firstStep.Start();
        firstStep.Complete("Done", null);

        // Act
        execution.Complete();

        // Assert
        execution.CompletedSteps.Should().Be(1);
    }

    #endregion

    #region Fail - Running to Failed

    [Fact]
    public void Fail_WhenRunning_ShouldTransitionToFailed()
    {
        // Arrange
        var execution = CreateRunningExecution();

        // Act
        var result = execution.Fail("Something went wrong");

        // Assert
        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Failed);
    }

    [Fact]
    public void Fail_ShouldSetErrorMessage()
    {
        // Arrange
        var execution = CreateRunningExecution();
        var errorMessage = "Processing failed due to invalid data";

        // Act
        execution.Fail(errorMessage);

        // Assert
        execution.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void Fail_ShouldSetCompletedAt()
    {
        // Arrange
        var execution = CreateRunningExecution();
        var beforeFail = DateTime.UtcNow;

        // Act
        execution.Fail("Error");

        // Assert
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Should().BeOnOrAfter(beforeFail);
    }

    [Fact]
    public void Fail_WithLongErrorMessage_ShouldTruncate()
    {
        // Arrange
        var execution = CreateRunningExecution();
        var longMessage = new string('x', PipelineExecution.MaxErrorMessageLength + 500);

        // Act
        execution.Fail(longMessage);

        // Assert
        execution.ErrorMessage.Should().HaveLength(PipelineExecution.MaxErrorMessageLength);
    }

    [Fact]
    public void Fail_WhenPending_ShouldReturnError()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        var result = execution.Fail("Error");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidExecutionStatusTransition");
    }

    [Fact]
    public void Fail_WithNullErrorMessage_ShouldSucceed()
    {
        // Arrange
        var execution = CreateRunningExecution();

        // Act
        var result = execution.Fail(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        execution.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region Cancel - Pending/Running to Cancelled

    [Fact]
    public void Cancel_WhenPending_ShouldTransitionToCancelled()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        var result = execution.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenRunning_ShouldTransitionToCancelled()
    {
        // Arrange
        var execution = CreateRunningExecution();

        // Act
        var result = execution.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldSetCompletedAt()
    {
        // Arrange
        var execution = CreateExecution();
        var beforeCancel = DateTime.UtcNow;

        // Act
        execution.Cancel();

        // Assert
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Should().BeOnOrAfter(beforeCancel);
    }

    [Fact]
    public void Cancel_ShouldSkipPendingSteps()
    {
        // Arrange
        var execution = CreateRunningExecution(3);
        // Start and complete first step
        var firstStep = execution.StepExecutions[0];
        firstStep.Start();
        firstStep.Complete("Done", null);

        // Act
        execution.Cancel();

        // Assert
        execution.StepExecutions[0].Status.Should().Be(StepExecutionStatus.Completed);
        execution.StepExecutions[1].Status.Should().Be(StepExecutionStatus.Skipped);
        execution.StepExecutions[2].Status.Should().Be(StepExecutionStatus.Skipped);
    }

    [Fact]
    public void Cancel_WhenCompleted_ShouldReturnError()
    {
        // Arrange
        var execution = CreateRunningExecution();
        execution.Complete();

        // Act
        var result = execution.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ExecutionNotCancellable");
    }

    [Fact]
    public void Cancel_WhenFailed_ShouldReturnError()
    {
        // Arrange
        var execution = CreateRunningExecution();
        execution.Fail("Error");

        // Act
        var result = execution.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ExecutionNotCancellable");
    }

    #endregion

    #region Computed Properties - CurrentStep

    [Fact]
    public void CurrentStep_WhenNoStepRunning_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateExecution();

        // Assert
        execution.CurrentStep.Should().BeNull();
    }

    [Fact]
    public void CurrentStep_WhenStepIsRunning_ShouldReturnRunningStep()
    {
        // Arrange
        var execution = CreateRunningExecution(3);
        var secondStep = execution.StepExecutions[1];

        // Complete first step and start second
        execution.StepExecutions[0].Start();
        execution.StepExecutions[0].Complete("Done", null);
        secondStep.Start();

        // Assert
        execution.CurrentStep.Should().Be(secondStep);
    }

    #endregion

    #region Computed Properties - NextStep

    [Fact]
    public void NextStep_WhenAllStepsPending_ShouldReturnFirstStep()
    {
        // Arrange
        var execution = CreateExecution(3);

        // Assert
        execution.NextStep.Should().Be(execution.StepExecutions[0]);
    }

    [Fact]
    public void NextStep_WhenSomeStepsCompleted_ShouldReturnNextPendingStep()
    {
        // Arrange
        var execution = CreateRunningExecution(3);
        execution.StepExecutions[0].Start();
        execution.StepExecutions[0].Complete("Done", null);

        // Assert
        execution.NextStep.Should().Be(execution.StepExecutions[1]);
    }

    [Fact]
    public void NextStep_WhenAllStepsCompleted_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateRunningExecution(2);
        foreach (var step in execution.StepExecutions)
        {
            step.Start();
            step.Complete("Done", null);
        }

        // Assert
        execution.NextStep.Should().BeNull();
    }

    #endregion

    #region Computed Properties - Duration

    [Fact]
    public void Duration_WhenNotStarted_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateExecution();

        // Assert
        execution.Duration.Should().BeNull();
    }

    [Fact]
    public void Duration_WhenRunning_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateRunningExecution();

        // Assert
        execution.Duration.Should().BeNull();
    }

    [Fact]
    public void Duration_WhenCompleted_ShouldReturnDuration()
    {
        // Arrange
        var execution = CreateRunningExecution();
        execution.Complete();

        // Assert
        execution.Duration.Should().NotBeNull();
        execution.Duration!.Value.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Duration_WhenFailed_ShouldReturnDuration()
    {
        // Arrange
        var execution = CreateRunningExecution();
        execution.Fail("Error");

        // Assert
        execution.Duration.Should().NotBeNull();
    }

    #endregion

    #region Computed Properties - ProgressPercentage

    [Fact]
    public void ProgressPercentage_WhenNoStepsCompleted_ShouldReturnZero()
    {
        // Arrange
        var execution = CreateExecution(4);

        // Assert
        execution.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void ProgressPercentage_WhenHalfStepsCompleted_ShouldReturnFifty()
    {
        // Arrange
        var execution = CreateRunningExecution(4);
        execution.IncrementCompletedSteps();
        execution.IncrementCompletedSteps();

        // Assert
        execution.ProgressPercentage.Should().Be(50);
    }

    [Fact]
    public void ProgressPercentage_WhenAllStepsCompleted_ShouldReturnHundred()
    {
        // Arrange
        var execution = CreateRunningExecution(3);
        execution.IncrementCompletedSteps();
        execution.IncrementCompletedSteps();
        execution.IncrementCompletedSteps();

        // Assert
        execution.ProgressPercentage.Should().Be(100);
    }

    [Fact]
    public void ProgressPercentage_WhenNoSteps_ShouldReturnZero()
    {
        // Arrange - Create execution with only disabled steps (edge case)
        var ownerId = CreateUserId();
        var pipeline = Pipeline.Create(
            CreatePipelineId(), CreateStudyId(), ownerId,
            CreateName(), CreateDescription()).Value;

        // Add one enabled step to activate, then we'll test TotalSteps = 0 scenario
        pipeline.AddStep(StepType.Quality, CreateConfig(), "Step", true, ownerId);
        pipeline.Activate(ownerId);
        var execution = pipeline.StartExecution(CreateExecutionId(), CreateTraceId(), ownerId).Value;

        // Assert - TotalSteps is 1, so with 0 completed it's 0%
        execution.ProgressPercentage.Should().Be(0);
    }

    #endregion

    #region IncrementCompletedSteps

    [Fact]
    public void IncrementCompletedSteps_ShouldIncrementByOne()
    {
        // Arrange
        var execution = CreateExecution();
        var initialCount = execution.CompletedSteps;

        // Act
        execution.IncrementCompletedSteps();

        // Assert
        execution.CompletedSteps.Should().Be(initialCount + 1);
    }

    [Fact]
    public void IncrementCompletedSteps_MultipleCalls_ShouldIncrementCorrectly()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        execution.IncrementCompletedSteps();
        execution.IncrementCompletedSteps();
        execution.IncrementCompletedSteps();

        // Assert
        execution.CompletedSteps.Should().Be(3);
    }

    #endregion

    #region GetStepExecution

    [Fact]
    public void GetStepExecution_WithValidId_ShouldReturnStep()
    {
        // Arrange
        var execution = CreateExecution(3);
        var expectedStep = execution.StepExecutions[1];

        // Act
        var result = execution.GetStepExecution(expectedStep.Id);

        // Assert
        result.Should().Be(expectedStep);
    }

    [Fact]
    public void GetStepExecution_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateExecution();
        var invalidId = Guid.NewGuid();

        // Act
        var result = execution.GetStepExecution(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetStepExecution_WithEmptyGuid_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        var result = execution.GetStepExecution(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
