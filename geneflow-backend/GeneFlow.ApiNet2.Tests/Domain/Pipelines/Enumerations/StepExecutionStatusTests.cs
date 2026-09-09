using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.Enumerations;

/// <summary>
/// Unit tests for StepExecutionStatus smart enumeration.
/// </summary>
public class StepExecutionStatusTests
{
    #region Static Values

    [Fact]
    public void Pending_ShouldHaveCorrectProperties()
    {
        // Assert
        StepExecutionStatus.Pending.Id.Should().Be(1);
        StepExecutionStatus.Pending.Name.Should().Be("Pending");
        StepExecutionStatus.Pending.DisplayName.Should().Be("Pending");
        StepExecutionStatus.Pending.IsInProgress.Should().BeFalse();
        StepExecutionStatus.Pending.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void Running_ShouldHaveCorrectProperties()
    {
        // Assert
        StepExecutionStatus.Running.Id.Should().Be(2);
        StepExecutionStatus.Running.Name.Should().Be("Running");
        StepExecutionStatus.Running.DisplayName.Should().Be("Running");
        StepExecutionStatus.Running.IsInProgress.Should().BeTrue();
        StepExecutionStatus.Running.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void Completed_ShouldHaveCorrectProperties()
    {
        // Assert
        StepExecutionStatus.Completed.Id.Should().Be(3);
        StepExecutionStatus.Completed.Name.Should().Be("Completed");
        StepExecutionStatus.Completed.DisplayName.Should().Be("Completed");
        StepExecutionStatus.Completed.IsInProgress.Should().BeFalse();
        StepExecutionStatus.Completed.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Failed_ShouldHaveCorrectProperties()
    {
        // Assert
        StepExecutionStatus.Failed.Id.Should().Be(4);
        StepExecutionStatus.Failed.Name.Should().Be("Failed");
        StepExecutionStatus.Failed.DisplayName.Should().Be("Failed");
        StepExecutionStatus.Failed.IsInProgress.Should().BeFalse();
        StepExecutionStatus.Failed.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Skipped_ShouldHaveCorrectProperties()
    {
        // Assert
        StepExecutionStatus.Skipped.Id.Should().Be(5);
        StepExecutionStatus.Skipped.Name.Should().Be("Skipped");
        StepExecutionStatus.Skipped.DisplayName.Should().Be("Skipped");
        StepExecutionStatus.Skipped.IsInProgress.Should().BeFalse();
        StepExecutionStatus.Skipped.IsTerminal.Should().BeTrue();
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllStatuses()
    {
        // Act
        var allStatuses = StepExecutionStatus.GetAll();

        // Assert
        allStatuses.Should().HaveCount(5);
        allStatuses.Should().Contain(StepExecutionStatus.Pending);
        allStatuses.Should().Contain(StepExecutionStatus.Running);
        allStatuses.Should().Contain(StepExecutionStatus.Completed);
        allStatuses.Should().Contain(StepExecutionStatus.Failed);
        allStatuses.Should().Contain(StepExecutionStatus.Skipped);
    }

    [Fact]
    public void All_ShouldHaveUniqueIds()
    {
        // Act
        var allStatuses = StepExecutionStatus.GetAll();
        var ids = allStatuses.Select(s => s.Id);

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Pending")]
    [InlineData(2, "Running")]
    [InlineData(3, "Completed")]
    [InlineData(4, "Failed")]
    [InlineData(5, "Skipped")]
    public void FromId_WithValidId_ShouldReturnCorrectStatus(int id, string expectedName)
    {
        // Act
        var status = StepExecutionStatus.FromId(id);

        // Assert
        status.Should().NotBeNull();
        status!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var status = StepExecutionStatus.FromId(999);

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region CanTransitionTo - Valid Transitions

    [Theory]
    [InlineData("Pending", "Running")]
    [InlineData("Pending", "Skipped")]
    [InlineData("Running", "Completed")]
    [InlineData("Running", "Failed")]
    public void CanTransitionTo_ValidTransition_ShouldReturnTrue(string fromName, string toName)
    {
        // Arrange
        var from = StepExecutionStatus.FromName(fromName)!;
        var to = StepExecutionStatus.FromName(toName)!;

        // Act
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().BeTrue();
    }

    #endregion

    #region CanTransitionTo - Invalid Transitions

    [Theory]
    [InlineData("Pending", "Completed")]
    [InlineData("Pending", "Failed")]
    [InlineData("Running", "Pending")]
    [InlineData("Running", "Skipped")]
    [InlineData("Completed", "Running")]
    [InlineData("Completed", "Failed")]
    [InlineData("Failed", "Running")]
    [InlineData("Failed", "Completed")]
    [InlineData("Skipped", "Running")]
    [InlineData("Skipped", "Completed")]
    public void CanTransitionTo_InvalidTransition_ShouldReturnFalse(string fromName, string toName)
    {
        // Arrange
        var from = StepExecutionStatus.FromName(fromName)!;
        var to = StepExecutionStatus.FromName(toName)!;

        // Act
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CanTransitionTo_SameStatus_ShouldReturnFalse()
    {
        // Assert - All statuses cannot transition to themselves
        foreach (var status in StepExecutionStatus.GetAll())
        {
            status.CanTransitionTo(status).Should().BeFalse();
        }
    }

    [Fact]
    public void TerminalStatuses_CannotTransition_ToAnyStatus()
    {
        // Arrange
        var terminalStatuses = StepExecutionStatus.GetAll().Where(s => s.IsTerminal);
        var allStatuses = StepExecutionStatus.GetAll();

        // Assert
        foreach (var terminal in terminalStatuses)
        {
            foreach (var target in allStatuses)
            {
                terminal.CanTransitionTo(target).Should().BeFalse();
            }
        }
    }

    #endregion

    #region IsSuccess

    [Fact]
    public void IsSuccess_WhenCompleted_ShouldReturnTrue()
    {
        // Assert
        StepExecutionStatus.Completed.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Running")]
    [InlineData("Failed")]
    [InlineData("Skipped")]
    public void IsSuccess_WhenNotCompleted_ShouldReturnFalse(string statusName)
    {
        // Arrange
        var status = StepExecutionStatus.FromName(statusName)!;

        // Assert
        status.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region IsInProgress

    [Fact]
    public void IsInProgress_OnlyRunning_ShouldReturnTrue()
    {
        // Assert
        var inProgressStatuses = StepExecutionStatus.GetAll().Where(s => s.IsInProgress);
        inProgressStatuses.Should().ContainSingle().Which.Should().Be(StepExecutionStatus.Running);
    }

    #endregion

    #region IsTerminal

    [Fact]
    public void IsTerminal_ShouldReturnTrueForCompletedFailedSkipped()
    {
        // Assert
        var terminalStatuses = StepExecutionStatus.GetAll().Where(s => s.IsTerminal);
        terminalStatuses.Should().HaveCount(3);
        terminalStatuses.Should().Contain(StepExecutionStatus.Completed);
        terminalStatuses.Should().Contain(StepExecutionStatus.Failed);
        terminalStatuses.Should().Contain(StepExecutionStatus.Skipped);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameStatus_ShouldReturnTrue()
    {
        // Arrange
        var status1 = StepExecutionStatus.Running;
        var status2 = StepExecutionStatus.FromId(2);

        // Assert
        (status1 == status2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentStatus_ShouldReturnFalse()
    {
        // Arrange
        var status1 = StepExecutionStatus.Pending;
        var status2 = StepExecutionStatus.Running;

        // Assert
        (status1 != status2).Should().BeTrue();
    }

    #endregion
}
