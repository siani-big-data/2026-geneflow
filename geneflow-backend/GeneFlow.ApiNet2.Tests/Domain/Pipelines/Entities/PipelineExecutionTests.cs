using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Traces;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.Entities;

/// <summary>
/// Unit tests for the PipelineExecution entity.
/// </summary>
public class PipelineExecutionTests
{
    private static PipelineExecutionId CreateExecutionId(long value = 1) => new(value);
    private static PipelineId CreatePipelineId(long value = 1) => new(value);
    private static TraceId CreateTraceId() => TraceId.New();
    private static UserId CreateUserId(long value = 1) => new(value);
    private static StepConfiguration CreateConfig() => StepConfiguration.Create("{}", StepType.Quality).Value;

    private static List<PipelineStep> CreateSteps(int count = 2, bool allEnabled = true)
    {
        var steps = new List<PipelineStep>();
        for (int i = 0; i < count; i++)
        {
            steps.Add(PipelineStep.Create(StepType.Quality, i + 1, $"Step {i + 1}", CreateConfig(), allEnabled));
        }
        return steps;
    }

    private static PipelineExecution CreateExecution(int stepCount = 2) =>
        PipelineExecution.Create(
            CreateExecutionId(),
            CreatePipelineId(),
            CreateTraceId(),
            CreateUserId(),
            CreateSteps(stepCount));

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldCreateExecution()
    {
        // Arrange
        var executionId = CreateExecutionId();
        var pipelineId = CreatePipelineId();
        var traceId = CreateTraceId();
        var userId = CreateUserId();
        var steps = CreateSteps(3);

        // Act
        var execution = PipelineExecution.Create(executionId, pipelineId, traceId, userId, steps);

        // Assert
        execution.Should().NotBeNull();
        execution.Id.Should().Be(executionId);
        execution.PipelineId.Should().Be(pipelineId);
        execution.TraceId.Should().Be(traceId);
        execution.StartedBy.Should().Be(userId);
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
    public void Create_ShouldInitializeStepExecutions()
    {
        // Arrange
        var steps = CreateSteps(3);

        // Act
        var execution = PipelineExecution.Create(
            CreateExecutionId(), CreatePipelineId(), CreateTraceId(), CreateUserId(), steps);

        // Assert
        execution.StepExecutions.Should().HaveCount(3);
        execution.TotalSteps.Should().Be(3);
    }

    [Fact]
    public void Create_ShouldOnlyIncludeEnabledSteps()
    {
        // Arrange
        var steps = new List<PipelineStep>
        {
            PipelineStep.Create(StepType.Quality, 1, "Enabled", CreateConfig(), true),
            PipelineStep.Create(StepType.Quality, 2, "Disabled", CreateConfig(), false),
            PipelineStep.Create(StepType.Quality, 3, "Enabled", CreateConfig(), true)
        };

        // Act
        var execution = PipelineExecution.Create(
            CreateExecutionId(), CreatePipelineId(), CreateTraceId(), CreateUserId(), steps);

        // Assert
        execution.StepExecutions.Should().HaveCount(2);
        execution.TotalSteps.Should().Be(2);
    }

    [Fact]
    public void Create_ShouldNotSetStartedAt()
    {
        // Act
        var execution = CreateExecution();

        // Assert
        execution.StartedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetCompletedStepsToZero()
    {
        // Act
        var execution = CreateExecution();

        // Assert
        execution.CompletedSteps.Should().Be(0);
    }

    #endregion

    #region Start

    [Fact]
    public void Start_ShouldSetStatusToRunning()
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
        var execution = CreateExecution();
        execution.Start();

        // Act
        var result = execution.Start();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidExecutionStatusTransition");
    }

    #endregion

    #region Complete

    [Fact]
    public void Complete_WhenAllStepsComplete_ShouldComplete()
    {
        // Arrange
        var execution = CreateExecution(2);
        execution.Start();
        foreach (var step in execution.StepExecutions)
        {
            step.Start();
            step.Complete(null, null);
        }
        execution.IncrementCompletedSteps();
        execution.IncrementCompletedSteps();

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
        var execution = CreateExecution(1);
        execution.Start();
        execution.StepExecutions.First().Start();
        execution.StepExecutions.First().Complete(null, null);
        var beforeComplete = DateTime.UtcNow;

        // Act
        execution.Complete();

        // Assert
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Should().BeOnOrAfter(beforeComplete);
    }

    [Fact]
    public void Complete_FromPending_ShouldReturnError()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        var result = execution.Complete();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Fail

    [Fact]
    public void Fail_WithError_ShouldFail()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Act
        var result = execution.Fail("An error occurred");

        // Assert
        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Failed);
        execution.ErrorMessage.Should().Be("An error occurred");
    }

    [Fact]
    public void Fail_ShouldSetCompletedAt()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        var beforeFail = DateTime.UtcNow;

        // Act
        execution.Fail("Error");

        // Assert
        execution.CompletedAt.Should().NotBeNull();
        execution.CompletedAt.Should().BeOnOrAfter(beforeFail);
    }

    [Fact]
    public void Fail_ShouldNotAffectPendingStepsStatus()
    {
        // Arrange
        var execution = CreateExecution(3);
        execution.Start();
        execution.StepExecutions[0].Start();
        // Steps 1 and 2 are still pending

        // Act
        execution.Fail("Error");

        // Assert - Pending steps remain Pending (they are not automatically skipped)
        execution.StepExecutions[1].Status.Should().Be(StepExecutionStatus.Pending);
        execution.StepExecutions[2].Status.Should().Be(StepExecutionStatus.Pending);
    }

    [Fact]
    public void Fail_TruncatesLongErrorMessage()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        var longError = new string('e', PipelineExecution.MaxErrorMessageLength + 100);

        // Act
        execution.Fail(longError);

        // Assert
        execution.ErrorMessage.Should().HaveLength(PipelineExecution.MaxErrorMessageLength);
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_WhenRunning_ShouldCancel()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Act
        var result = execution.Cancel();

        // Assert
        result.IsSuccess.Should().BeTrue();
        execution.Status.Should().Be(ExecutionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenPending_ShouldCancel()
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
    public void Cancel_WhenCompleted_ShouldReturnError()
    {
        // Arrange
        var execution = CreateExecution(1);
        execution.Start();
        execution.StepExecutions.First().Start();
        execution.StepExecutions.First().Complete(null, null);
        execution.Complete();

        // Act
        var result = execution.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_WhenFailed_ShouldReturnError()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();
        execution.Fail("Error");

        // Act
        var result = execution.Cancel();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_ShouldSkipPendingSteps()
    {
        // Arrange
        var execution = CreateExecution(3);
        execution.Start();
        // All steps are still pending

        // Act
        execution.Cancel();

        // Assert
        foreach (var step in execution.StepExecutions)
        {
            step.Status.Should().Be(StepExecutionStatus.Skipped);
        }
    }

    #endregion

    #region GetStepExecution

    [Fact]
    public void GetStepExecution_ById_ShouldReturnStep()
    {
        // Arrange
        var execution = CreateExecution(3);
        var stepId = execution.StepExecutions[1].Id;

        // Act
        var step = execution.GetStepExecution(stepId);

        // Assert
        step.Should().NotBeNull();
        step!.Id.Should().Be(stepId);
    }

    [Fact]
    public void GetStepExecution_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateExecution();

        // Act
        var step = execution.GetStepExecution(Guid.NewGuid());

        // Assert
        step.Should().BeNull();
    }

    #endregion

    #region NextStep

    [Fact]
    public void NextStep_ShouldReturnNextPendingStep()
    {
        // Arrange
        var execution = CreateExecution(3);
        execution.Start();
        execution.StepExecutions[0].Start();
        execution.StepExecutions[0].Complete(null, null);

        // Act
        var nextStep = execution.NextStep;

        // Assert
        nextStep.Should().NotBeNull();
        nextStep!.Order.Should().Be(2);
    }

    [Fact]
    public void NextStep_WhenAllCompleted_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateExecution(2);
        execution.Start();
        foreach (var step in execution.StepExecutions)
        {
            step.Start();
            step.Complete(null, null);
        }

        // Act
        var nextStep = execution.NextStep;

        // Assert
        nextStep.Should().BeNull();
    }

    #endregion

    #region CurrentStep

    [Fact]
    public void CurrentStep_ShouldReturnRunningStep()
    {
        // Arrange
        var execution = CreateExecution(3);
        execution.Start();
        execution.StepExecutions[1].Start();

        // Act
        var currentStep = execution.CurrentStep;

        // Assert
        currentStep.Should().NotBeNull();
        currentStep!.Order.Should().Be(2);
    }

    [Fact]
    public void CurrentStep_WhenNoRunningStep_ShouldReturnNull()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Act
        var currentStep = execution.CurrentStep;

        // Assert
        currentStep.Should().BeNull();
    }

    #endregion

    #region Duration

    [Fact]
    public void Duration_WhenCompleted_ShouldCalculateDuration()
    {
        // Arrange
        var execution = CreateExecution(1);
        execution.Start();
        Thread.Sleep(10);
        execution.StepExecutions.First().Start();
        execution.StepExecutions.First().Complete(null, null);
        execution.Complete();

        // Assert
        execution.Duration.Should().NotBeNull();
        execution.Duration!.Value.TotalMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Duration_WhenNotCompleted_ShouldBeNull()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Assert
        execution.Duration.Should().BeNull();
    }

    #endregion

    #region ProgressPercentage

    [Fact]
    public void ProgressPercentage_ShouldReturnCorrectPercentage()
    {
        // Arrange
        var execution = CreateExecution(4);
        execution.Start();
        execution.IncrementCompletedSteps();
        execution.IncrementCompletedSteps();

        // Assert
        execution.ProgressPercentage.Should().Be(50);
    }

    [Fact]
    public void ProgressPercentage_WhenNoSteps_ShouldReturnZero()
    {
        // Arrange - Create execution with no enabled steps
        var steps = new List<PipelineStep>
        {
            PipelineStep.Create(StepType.Quality, 1, "Disabled", CreateConfig(), false)
        };
        var execution = PipelineExecution.Create(
            CreateExecutionId(), CreatePipelineId(), CreateTraceId(), CreateUserId(), steps);

        // Assert
        execution.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void ProgressPercentage_WhenComplete_ShouldReturn100()
    {
        // Arrange
        var execution = CreateExecution(2);
        execution.Start();
        execution.IncrementCompletedSteps();
        execution.IncrementCompletedSteps();

        // Assert
        execution.ProgressPercentage.Should().Be(100);
    }

    #endregion

    #region IncrementCompletedSteps

    [Fact]
    public void IncrementCompletedSteps_ShouldIncreaseCount()
    {
        // Arrange
        var execution = CreateExecution();
        execution.Start();

        // Act
        execution.IncrementCompletedSteps();

        // Assert
        execution.CompletedSteps.Should().Be(1);
    }

    #endregion

    #region Constants

    [Fact]
    public void MaxErrorMessageLength_ShouldBe2000()
    {
        // Assert
        PipelineExecution.MaxErrorMessageLength.Should().Be(2000);
    }

    #endregion
}
