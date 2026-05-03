using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.Entities;

/// <summary>
/// Unit tests for the StepExecution entity.
/// </summary>
public class StepExecutionTests
{
    private static StepExecution CreateStepExecution(StepType? stepType = null) =>
        StepExecution.Create(Guid.NewGuid(), 1, stepType ?? StepType.Quality);

    #region Create

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var step = CreateStepExecution();

        // Assert
        step.Status.Should().Be(StepExecutionStatus.Pending);
    }

    [Fact]
    public void Create_ShouldSetCorrectProperties()
    {
        // Arrange
        var pipelineStepId = Guid.NewGuid();
        const int stepOrder = 3;
        var stepType = StepType.Trimming;

        // Act
        var step = StepExecution.Create(pipelineStepId, stepOrder, stepType);

        // Assert
        step.PipelineStepId.Should().Be(pipelineStepId);
        step.Order.Should().Be(stepOrder);
        step.StepType.Should().Be(stepType);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueId()
    {
        // Act
        var step1 = CreateStepExecution();
        var step2 = CreateStepExecution();

        // Assert
        step1.Id.Should().NotBe(step2.Id);
        step1.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldNotSetStartedAt()
    {
        // Act
        var step = CreateStepExecution();

        // Assert
        step.StartedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNotSetCompletedAt()
    {
        // Act
        var step = CreateStepExecution();

        // Assert
        step.CompletedAt.Should().BeNull();
    }

    #endregion

    #region Start

    [Fact]
    public void Start_ShouldSetStatusToRunning()
    {
        // Arrange
        var step = CreateStepExecution();

        // Act
        var result = step.Start();

        // Assert
        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(StepExecutionStatus.Running);
    }

    [Fact]
    public void Start_ShouldSetStartedAt()
    {
        // Arrange
        var step = CreateStepExecution();
        var beforeStart = DateTime.UtcNow;

        // Act
        step.Start();

        // Assert
        step.StartedAt.Should().NotBeNull();
        step.StartedAt.Should().BeOnOrAfter(beforeStart);
    }

    [Fact]
    public void Start_WhenAlreadyRunning_ShouldReturnError()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();

        // Act
        var result = step.Start();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStepExecutionStatusTransition");
    }

    [Fact]
    public void Start_WhenCompleted_ShouldReturnError()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();
        step.Complete(null, null);

        // Act
        var result = step.Start();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Complete

    [Fact]
    public void Complete_WithResult_ShouldComplete()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();

        // Act
        var result = step.Complete("Analysis complete", """{"quality": 95}""");

        // Assert
        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(StepExecutionStatus.Completed);
        step.ResultSummary.Should().Be("Analysis complete");
        step.ResultData.Should().Be("""{"quality": 95}""");
    }

    [Fact]
    public void Complete_ShouldSetCompletedAt()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();
        var beforeComplete = DateTime.UtcNow;

        // Act
        step.Complete(null, null);

        // Assert
        step.CompletedAt.Should().NotBeNull();
        step.CompletedAt.Should().BeOnOrAfter(beforeComplete);
    }

    [Fact]
    public void Complete_WithNullResult_ShouldComplete()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();

        // Act
        var result = step.Complete(null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        step.ResultSummary.Should().BeNull();
        step.ResultData.Should().BeNull();
    }

    [Fact]
    public void Complete_FromPending_ShouldReturnError()
    {
        // Arrange
        var step = CreateStepExecution();

        // Act
        var result = step.Complete("Result", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStepExecutionStatusTransition");
    }

    [Fact]
    public void Complete_TruncatesLongResultSummary()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();
        var longSummary = new string('a', StepExecution.MaxResultSummaryLength + 100);

        // Act
        step.Complete(longSummary, null);

        // Assert
        step.ResultSummary.Should().HaveLength(StepExecution.MaxResultSummaryLength);
    }

    #endregion

    #region Fail

    [Fact]
    public void Fail_WithError_ShouldFail()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();

        // Act
        var result = step.Fail("An error occurred");

        // Assert
        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(StepExecutionStatus.Failed);
        step.ErrorMessage.Should().Be("An error occurred");
    }

    [Fact]
    public void Fail_ShouldSetCompletedAt()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();
        var beforeFail = DateTime.UtcNow;

        // Act
        step.Fail("Error");

        // Assert
        step.CompletedAt.Should().NotBeNull();
        step.CompletedAt.Should().BeOnOrAfter(beforeFail);
    }

    [Fact]
    public void Fail_FromPending_ShouldReturnError()
    {
        // Arrange
        var step = CreateStepExecution();

        // Act
        var result = step.Fail("Error");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Fail_TruncatesLongErrorMessage()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();
        var longError = new string('e', StepExecution.MaxErrorMessageLength + 100);

        // Act
        step.Fail(longError);

        // Assert
        step.ErrorMessage.Should().HaveLength(StepExecution.MaxErrorMessageLength);
    }

    #endregion

    #region Skip

    [Fact]
    public void Skip_ShouldSetStatusToSkipped()
    {
        // Arrange
        var step = CreateStepExecution();

        // Act
        var result = step.Skip();

        // Assert
        result.IsSuccess.Should().BeTrue();
        step.Status.Should().Be(StepExecutionStatus.Skipped);
    }

    [Fact]
    public void Skip_ShouldSetCompletedAt()
    {
        // Arrange
        var step = CreateStepExecution();
        var beforeSkip = DateTime.UtcNow;

        // Act
        step.Skip();

        // Assert
        step.CompletedAt.Should().NotBeNull();
        step.CompletedAt.Should().BeOnOrAfter(beforeSkip);
    }

    [Fact]
    public void Skip_FromRunning_ShouldReturnError()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();

        // Act
        var result = step.Skip();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Duration

    [Fact]
    public void Duration_WhenCompleted_ShouldCalculateCorrectly()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();
        // Simulate some time passing
        Thread.Sleep(10);
        step.Complete(null, null);

        // Assert
        step.Duration.Should().NotBeNull();
        step.Duration!.Value.TotalMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Duration_WhenNotCompleted_ShouldBeNull()
    {
        // Arrange
        var step = CreateStepExecution();
        step.Start();

        // Assert
        step.Duration.Should().BeNull();
    }

    [Fact]
    public void Duration_WhenNotStarted_ShouldBeNull()
    {
        // Arrange
        var step = CreateStepExecution();

        // Assert
        step.Duration.Should().BeNull();
    }

    #endregion

    #region State Transitions

    [Fact]
    public void CanTransitionTo_ValidTransitions_ShouldSucceed()
    {
        // Pending -> Running
        var step1 = CreateStepExecution();
        step1.Start().IsSuccess.Should().BeTrue();

        // Pending -> Skipped
        var step2 = CreateStepExecution();
        step2.Skip().IsSuccess.Should().BeTrue();

        // Running -> Completed
        var step3 = CreateStepExecution();
        step3.Start();
        step3.Complete(null, null).IsSuccess.Should().BeTrue();

        // Running -> Failed
        var step4 = CreateStepExecution();
        step4.Start();
        step4.Fail("Error").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanTransitionTo_InvalidTransitions_ShouldFail()
    {
        // Completed -> Running
        var step1 = CreateStepExecution();
        step1.Start();
        step1.Complete(null, null);
        step1.Start().IsFailure.Should().BeTrue();

        // Failed -> Completed
        var step2 = CreateStepExecution();
        step2.Start();
        step2.Fail("Error");
        step2.Complete(null, null).IsFailure.Should().BeTrue();

        // Skipped -> Running
        var step3 = CreateStepExecution();
        step3.Skip();
        step3.Start().IsFailure.Should().BeTrue();
    }

    #endregion

    #region Constants

    [Fact]
    public void MaxErrorMessageLength_ShouldBe2000()
    {
        // Assert
        StepExecution.MaxErrorMessageLength.Should().Be(2000);
    }

    [Fact]
    public void MaxResultSummaryLength_ShouldBe4000()
    {
        // Assert
        StepExecution.MaxResultSummaryLength.Should().Be(4000);
    }

    #endregion
}
