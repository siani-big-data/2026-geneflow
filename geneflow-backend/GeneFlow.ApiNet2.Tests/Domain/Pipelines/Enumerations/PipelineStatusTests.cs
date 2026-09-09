using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.Enumerations;

/// <summary>
/// Unit tests for PipelineStatus smart enumeration.
/// </summary>
public class PipelineStatusTests
{
    #region Static Values

    [Fact]
    public void Draft_ShouldHaveCorrectIdAndName()
    {
        // Assert
        PipelineStatus.Draft.Id.Should().Be(1);
        PipelineStatus.Draft.Name.Should().Be("Draft");
        PipelineStatus.Draft.DisplayName.Should().Be("Draft");
    }

    [Fact]
    public void Active_ShouldHaveCorrectIdAndName()
    {
        // Assert
        PipelineStatus.Active.Id.Should().Be(2);
        PipelineStatus.Active.Name.Should().Be("Active");
        PipelineStatus.Active.DisplayName.Should().Be("Active");
    }

    [Fact]
    public void Archived_ShouldHaveCorrectIdAndName()
    {
        // Assert
        PipelineStatus.Archived.Id.Should().Be(3);
        PipelineStatus.Archived.Name.Should().Be("Archived");
        PipelineStatus.Archived.DisplayName.Should().Be("Archived");
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllStatuses()
    {
        // Act
        var allStatuses = PipelineStatus.GetAll();

        // Assert
        allStatuses.Should().HaveCount(3);
        allStatuses.Should().Contain(PipelineStatus.Draft);
        allStatuses.Should().Contain(PipelineStatus.Active);
        allStatuses.Should().Contain(PipelineStatus.Archived);
    }

    [Fact]
    public void All_ShouldHaveUniqueIds()
    {
        // Act
        var allStatuses = PipelineStatus.GetAll();
        var ids = allStatuses.Select(s => s.Id);

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ShouldHaveUniqueNames()
    {
        // Act
        var allStatuses = PipelineStatus.GetAll();
        var names = allStatuses.Select(s => s.Name);

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Draft")]
    [InlineData(2, "Active")]
    [InlineData(3, "Archived")]
    public void FromId_WithValidId_ShouldReturnCorrectStatus(int id, string expectedName)
    {
        // Act
        var status = PipelineStatus.FromId(id);

        // Assert
        status.Should().NotBeNull();
        status!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var status = PipelineStatus.FromId(999);

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Draft", 1)]
    [InlineData("Active", 2)]
    [InlineData("Archived", 3)]
    public void FromName_WithValidName_ShouldReturnCorrectStatus(string name, int expectedId)
    {
        // Act
        var status = PipelineStatus.FromName(name);

        // Assert
        status.Should().NotBeNull();
        status!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var status = PipelineStatus.FromName("InvalidStatus");

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region CanTransitionTo - Valid Transitions

    [Theory]
    [InlineData("Draft", "Active")]
    [InlineData("Draft", "Archived")]
    [InlineData("Active", "Draft")]
    [InlineData("Active", "Archived")]
    [InlineData("Archived", "Draft")]
    public void CanTransitionTo_ValidTransition_ShouldReturnTrue(string fromName, string toName)
    {
        // Arrange
        var from = PipelineStatus.FromName(fromName)!;
        var to = PipelineStatus.FromName(toName)!;

        // Act
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().BeTrue();
    }

    #endregion

    #region CanTransitionTo - Invalid Transitions

    [Theory]
    [InlineData("Archived", "Active")]
    public void CanTransitionTo_InvalidTransition_ShouldReturnFalse(string fromName, string toName)
    {
        // Arrange
        var from = PipelineStatus.FromName(fromName)!;
        var to = PipelineStatus.FromName(toName)!;

        // Act
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public void CanTransitionTo_SameStatus_ShouldReturnFalse()
    {
        // Assert
        PipelineStatus.Draft.CanTransitionTo(PipelineStatus.Draft).Should().BeFalse();
        PipelineStatus.Active.CanTransitionTo(PipelineStatus.Active).Should().BeFalse();
        PipelineStatus.Archived.CanTransitionTo(PipelineStatus.Archived).Should().BeFalse();
    }

    #endregion

    #region CanExecute

    [Fact]
    public void CanExecute_WhenActive_ShouldReturnTrue()
    {
        // Assert
        PipelineStatus.Active.CanExecute.Should().BeTrue();
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Archived")]
    public void CanExecute_WhenNotActive_ShouldReturnFalse(string statusName)
    {
        // Arrange
        var status = PipelineStatus.FromName(statusName)!;

        // Assert
        status.CanExecute.Should().BeFalse();
    }

    #endregion

    #region CanEdit

    [Fact]
    public void CanEdit_WhenDraft_ShouldReturnTrue()
    {
        // Assert
        PipelineStatus.Draft.CanEdit.Should().BeTrue();
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Archived")]
    public void CanEdit_WhenNotDraft_ShouldReturnFalse(string statusName)
    {
        // Arrange
        var status = PipelineStatus.FromName(statusName)!;

        // Assert
        status.CanEdit.Should().BeFalse();
    }

    #endregion

    #region CanDelete

    [Theory]
    [InlineData("Draft", true)]
    [InlineData("Active", true)]
    [InlineData("Archived", false)]
    public void CanDelete_ShouldReturnCorrectValue(string statusName, bool expected)
    {
        // Arrange
        var status = PipelineStatus.FromName(statusName)!;

        // Assert
        status.CanDelete.Should().Be(expected);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameStatus_ShouldReturnTrue()
    {
        // Arrange
        var status1 = PipelineStatus.Draft;
        var status2 = PipelineStatus.FromId(1);

        // Assert
        (status1 == status2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentStatus_ShouldReturnFalse()
    {
        // Arrange
        var status1 = PipelineStatus.Draft;
        var status2 = PipelineStatus.Active;

        // Assert
        (status1 != status2).Should().BeTrue();
    }

    #endregion
}
