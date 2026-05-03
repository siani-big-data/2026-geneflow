using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.Enumerations;

/// <summary>
/// Unit tests for ExecutionStatus smart enumeration.
/// </summary>
public class ExecutionStatusTests
{
    #region Static Values

    [Fact]
    public void Pending_ShouldHaveCorrectProperties()
    {
        // Assert
        ExecutionStatus.Pending.Id.Should().Be(1);
        ExecutionStatus.Pending.Name.Should().Be("Pending");
        ExecutionStatus.Pending.DisplayName.Should().Be("Pending");
        ExecutionStatus.Pending.IsInProgress.Should().BeFalse();
        ExecutionStatus.Pending.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void Running_ShouldHaveCorrectProperties()
    {
        // Assert
        ExecutionStatus.Running.Id.Should().Be(2);
        ExecutionStatus.Running.Name.Should().Be("Running");
        ExecutionStatus.Running.DisplayName.Should().Be("Running");
        ExecutionStatus.Running.IsInProgress.Should().BeTrue();
        ExecutionStatus.Running.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void Completed_ShouldHaveCorrectProperties()
    {
        // Assert
        ExecutionStatus.Completed.Id.Should().Be(3);
        ExecutionStatus.Completed.Name.Should().Be("Completed");
        ExecutionStatus.Completed.DisplayName.Should().Be("Completed");
        ExecutionStatus.Completed.IsInProgress.Should().BeFalse();
        ExecutionStatus.Completed.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Failed_ShouldHaveCorrectProperties()
    {
        // Assert
        ExecutionStatus.Failed.Id.Should().Be(4);
        ExecutionStatus.Failed.Name.Should().Be("Failed");
        ExecutionStatus.Failed.DisplayName.Should().Be("Failed");
        ExecutionStatus.Failed.IsInProgress.Should().BeFalse();
        ExecutionStatus.Failed.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void Cancelled_ShouldHaveCorrectProperties()
    {
        // Assert
        ExecutionStatus.Cancelled.Id.Should().Be(5);
        ExecutionStatus.Cancelled.Name.Should().Be("Cancelled");
        ExecutionStatus.Cancelled.DisplayName.Should().Be("Cancelled");
        ExecutionStatus.Cancelled.IsInProgress.Should().BeFalse();
        ExecutionStatus.Cancelled.IsTerminal.Should().BeTrue();
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllStatuses()
    {
        // Act
        var allStatuses = ExecutionStatus.GetAll();

        // Assert
        allStatuses.Should().HaveCount(5);
        allStatuses.Should().Contain(ExecutionStatus.Pending);
        allStatuses.Should().Contain(ExecutionStatus.Running);
        allStatuses.Should().Contain(ExecutionStatus.Completed);
        allStatuses.Should().Contain(ExecutionStatus.Failed);
        allStatuses.Should().Contain(ExecutionStatus.Cancelled);
    }

    [Fact]
    public void All_ShouldHaveUniqueIds()
    {
        // Act
        var allStatuses = ExecutionStatus.GetAll();
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
    [InlineData(5, "Cancelled")]
    public void FromId_WithValidId_ShouldReturnCorrectStatus(int id, string expectedName)
    {
        // Act
        var status = ExecutionStatus.FromId(id);

        // Assert
        status.Should().NotBeNull();
        status!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var status = ExecutionStatus.FromId(999);

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region CanTransitionTo - Valid Transitions

    [Theory]
    [InlineData("Pending", "Running")]
    [InlineData("Pending", "Cancelled")]
    [InlineData("Running", "Completed")]
    [InlineData("Running", "Failed")]
    [InlineData("Running", "Cancelled")]
    public void CanTransitionTo_ValidTransition_ShouldReturnTrue(string fromName, string toName)
    {
        // Arrange
        var from = ExecutionStatus.FromName(fromName)!;
        var to = ExecutionStatus.FromName(toName)!;

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
    [InlineData("Completed", "Running")]
    [InlineData("Completed", "Failed")]
    [InlineData("Completed", "Cancelled")]
    [InlineData("Failed", "Running")]
    [InlineData("Failed", "Completed")]
    [InlineData("Cancelled", "Running")]
    public void CanTransitionTo_InvalidTransition_ShouldReturnFalse(string fromName, string toName)
    {
        // Arrange
        var from = ExecutionStatus.FromName(fromName)!;
        var to = ExecutionStatus.FromName(toName)!;

        // Act
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CanTransitionTo_SameStatus_ShouldReturnFalse()
    {
        // Assert - All statuses cannot transition to themselves
        foreach (var status in ExecutionStatus.GetAll())
        {
            status.CanTransitionTo(status).Should().BeFalse();
        }
    }

    [Fact]
    public void TerminalStatuses_CannotTransition_ToAnyStatus()
    {
        // Arrange
        var terminalStatuses = ExecutionStatus.GetAll().Where(s => s.IsTerminal);
        var allStatuses = ExecutionStatus.GetAll();

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

    #region CanCancel

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Running", true)]
    [InlineData("Completed", false)]
    [InlineData("Failed", false)]
    [InlineData("Cancelled", false)]
    public void CanCancel_ShouldReturnCorrectValue(string statusName, bool expected)
    {
        // Arrange
        var status = ExecutionStatus.FromName(statusName)!;

        // Assert
        status.CanCancel.Should().Be(expected);
    }

    #endregion

    #region IsInProgress

    [Fact]
    public void IsInProgress_OnlyRunning_ShouldReturnTrue()
    {
        // Assert
        var inProgressStatuses = ExecutionStatus.GetAll().Where(s => s.IsInProgress);
        inProgressStatuses.Should().ContainSingle().Which.Should().Be(ExecutionStatus.Running);
    }

    #endregion

    #region IsTerminal

    [Fact]
    public void IsTerminal_ShouldReturnTrueForCompletedFailedCancelled()
    {
        // Assert
        var terminalStatuses = ExecutionStatus.GetAll().Where(s => s.IsTerminal);
        terminalStatuses.Should().HaveCount(3);
        terminalStatuses.Should().Contain(ExecutionStatus.Completed);
        terminalStatuses.Should().Contain(ExecutionStatus.Failed);
        terminalStatuses.Should().Contain(ExecutionStatus.Cancelled);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameStatus_ShouldReturnTrue()
    {
        // Arrange
        var status1 = ExecutionStatus.Running;
        var status2 = ExecutionStatus.FromId(2);

        // Assert
        (status1 == status2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentStatus_ShouldReturnFalse()
    {
        // Arrange
        var status1 = ExecutionStatus.Pending;
        var status2 = ExecutionStatus.Running;

        // Assert
        (status1 != status2).Should().BeTrue();
    }

    #endregion
}
