using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.Enumerations;

/// <summary>
/// Unit tests for StudyStatus smart enumeration.
/// </summary>
public class StudyStatusTests
{
    #region Static Values

    [Fact]
    public void Draft_ShouldHaveCorrectIdAndName()
    {
        // Assert
        StudyStatus.Draft.Id.Should().Be(1);
        StudyStatus.Draft.Name.Should().Be("Draft");
        StudyStatus.Draft.DisplayName.Should().Be("Draft");
    }

    [Fact]
    public void Active_ShouldHaveCorrectIdAndName()
    {
        // Assert
        StudyStatus.Active.Id.Should().Be(2);
        StudyStatus.Active.Name.Should().Be("Active");
        StudyStatus.Active.DisplayName.Should().Be("Active");
    }

    [Fact]
    public void Completed_ShouldHaveCorrectIdAndName()
    {
        // Assert
        StudyStatus.Completed.Id.Should().Be(3);
        StudyStatus.Completed.Name.Should().Be("Completed");
        StudyStatus.Completed.DisplayName.Should().Be("Completed");
    }

    [Fact]
    public void Published_ShouldHaveCorrectIdAndName()
    {
        // Assert
        StudyStatus.Published.Id.Should().Be(4);
        StudyStatus.Published.Name.Should().Be("Published");
        StudyStatus.Published.DisplayName.Should().Be("Published");
    }

    [Fact]
    public void Archived_ShouldHaveCorrectIdAndName()
    {
        // Assert
        StudyStatus.Archived.Id.Should().Be(5);
        StudyStatus.Archived.Name.Should().Be("Archived");
        StudyStatus.Archived.DisplayName.Should().Be("Archived");
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllStatuses()
    {
        // Act
        var allStatuses = StudyStatus.GetAll();

        // Assert
        allStatuses.Should().HaveCount(5);
        allStatuses.Should().Contain(StudyStatus.Draft);
        allStatuses.Should().Contain(StudyStatus.Active);
        allStatuses.Should().Contain(StudyStatus.Completed);
        allStatuses.Should().Contain(StudyStatus.Published);
        allStatuses.Should().Contain(StudyStatus.Archived);
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Draft")]
    [InlineData(2, "Active")]
    [InlineData(3, "Completed")]
    [InlineData(4, "Published")]
    [InlineData(5, "Archived")]
    public void FromId_WithValidId_ShouldReturnCorrectStatus(int id, string expectedName)
    {
        // Act
        var status = StudyStatus.FromId(id);

        // Assert
        status.Should().NotBeNull();
        status!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var status = StudyStatus.FromId(999);

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Draft", 1)]
    [InlineData("Active", 2)]
    [InlineData("COMPLETED", 3)]
    [InlineData("published", 4)]
    public void FromName_WithValidName_ShouldReturnCorrectStatus(string name, int expectedId)
    {
        // Act
        var status = StudyStatus.FromName(name);

        // Assert
        status.Should().NotBeNull();
        status!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var status = StudyStatus.FromName("InvalidStatus");

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region CanTransitionTo

    [Theory]
    [InlineData("Draft", "Active", true)]
    [InlineData("Active", "Draft", true)]
    [InlineData("Active", "Completed", true)]
    [InlineData("Completed", "Active", true)]
    [InlineData("Completed", "Published", true)]
    [InlineData("Published", "Archived", true)]
    [InlineData("Archived", "Published", true)]
    public void CanTransitionTo_ValidTransition_ShouldReturnTrue(
        string fromName, string toName, bool expected)
    {
        // Arrange
        var from = StudyStatus.FromName(fromName)!;
        var to = StudyStatus.FromName(toName)!;

        // Act
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().Be(expected);
    }

    [Theory]
    [InlineData("Draft", "Completed")]
    [InlineData("Draft", "Published")]
    [InlineData("Draft", "Archived")]
    [InlineData("Active", "Published")]
    [InlineData("Active", "Archived")]
    [InlineData("Completed", "Draft")]
    [InlineData("Completed", "Archived")]
    [InlineData("Published", "Draft")]
    [InlineData("Published", "Active")]
    [InlineData("Published", "Completed")]
    [InlineData("Archived", "Draft")]
    [InlineData("Archived", "Active")]
    [InlineData("Archived", "Completed")]
    public void CanTransitionTo_InvalidTransition_ShouldReturnFalse(
        string fromName, string toName)
    {
        // Arrange
        var from = StudyStatus.FromName(fromName)!;
        var to = StudyStatus.FromName(toName)!;

        // Act
        var canTransition = from.CanTransitionTo(to);

        // Assert
        canTransition.Should().BeFalse();
    }

    #endregion

    #region IsPubliclyVisible

    [Fact]
    public void IsPubliclyVisible_Published_ShouldReturnTrue()
    {
        // Assert
        StudyStatus.Published.IsPubliclyVisible.Should().BeTrue();
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Active")]
    [InlineData("Completed")]
    [InlineData("Archived")]
    public void IsPubliclyVisible_NonPublished_ShouldReturnFalse(string statusName)
    {
        // Arrange
        var status = StudyStatus.FromName(statusName)!;

        // Assert
        status.IsPubliclyVisible.Should().BeFalse();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameStatus_ShouldReturnTrue()
    {
        // Arrange
        var status1 = StudyStatus.Draft;
        var status2 = StudyStatus.FromId(1);

        // Assert
        (status1 == status2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentStatus_ShouldReturnFalse()
    {
        // Arrange
        var status1 = StudyStatus.Draft;
        var status2 = StudyStatus.Active;

        // Assert
        (status1 != status2).Should().BeTrue();
    }

    #endregion
}
