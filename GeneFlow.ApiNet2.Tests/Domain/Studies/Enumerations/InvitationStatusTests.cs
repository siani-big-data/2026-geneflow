using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.Enumerations;

/// <summary>
/// Unit tests for InvitationStatus smart enumeration.
/// </summary>
public class InvitationStatusTests
{
    #region Static Values

    [Fact]
    public void Pending_ShouldHaveCorrectValues()
    {
        // Assert
        InvitationStatus.Pending.Id.Should().Be(1);
        InvitationStatus.Pending.Name.Should().Be("Pending");
        InvitationStatus.Pending.DisplayName.Should().Be("Pending");
    }

    [Fact]
    public void Accepted_ShouldHaveCorrectValues()
    {
        // Assert
        InvitationStatus.Accepted.Id.Should().Be(2);
        InvitationStatus.Accepted.Name.Should().Be("Accepted");
        InvitationStatus.Accepted.DisplayName.Should().Be("Accepted");
    }

    [Fact]
    public void Declined_ShouldHaveCorrectValues()
    {
        // Assert
        InvitationStatus.Declined.Id.Should().Be(3);
        InvitationStatus.Declined.Name.Should().Be("Declined");
        InvitationStatus.Declined.DisplayName.Should().Be("Declined");
    }

    [Fact]
    public void Expired_ShouldHaveCorrectValues()
    {
        // Assert
        InvitationStatus.Expired.Id.Should().Be(4);
        InvitationStatus.Expired.Name.Should().Be("Expired");
        InvitationStatus.Expired.DisplayName.Should().Be("Expired");
    }

    [Fact]
    public void Cancelled_ShouldHaveCorrectValues()
    {
        // Assert
        InvitationStatus.Cancelled.Id.Should().Be(5);
        InvitationStatus.Cancelled.Name.Should().Be("Cancelled");
        InvitationStatus.Cancelled.DisplayName.Should().Be("Cancelled");
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllStatuses()
    {
        // Act
        var allStatuses = InvitationStatus.GetAll();

        // Assert
        allStatuses.Should().HaveCount(5);
        allStatuses.Should().Contain(InvitationStatus.Pending);
        allStatuses.Should().Contain(InvitationStatus.Accepted);
        allStatuses.Should().Contain(InvitationStatus.Declined);
        allStatuses.Should().Contain(InvitationStatus.Expired);
        allStatuses.Should().Contain(InvitationStatus.Cancelled);
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Pending")]
    [InlineData(2, "Accepted")]
    [InlineData(3, "Declined")]
    [InlineData(4, "Expired")]
    [InlineData(5, "Cancelled")]
    public void FromId_WithValidId_ShouldReturnCorrectStatus(int id, string expectedName)
    {
        // Act
        var status = InvitationStatus.FromId(id);

        // Assert
        status.Should().NotBeNull();
        status!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var status = InvitationStatus.FromId(999);

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Pending", 1)]
    [InlineData("ACCEPTED", 2)]
    [InlineData("declined", 3)]
    public void FromName_WithValidName_ShouldReturnCorrectStatus(string name, int expectedId)
    {
        // Act
        var status = InvitationStatus.FromName(name);

        // Assert
        status.Should().NotBeNull();
        status!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var status = InvitationStatus.FromName("InvalidStatus");

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region IsFinal

    [Fact]
    public void IsFinal_Pending_ShouldReturnFalse()
    {
        // Assert
        InvitationStatus.Pending.IsFinal.Should().BeFalse();
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Declined")]
    [InlineData("Expired")]
    [InlineData("Cancelled")]
    public void IsFinal_NonPendingStatuses_ShouldReturnTrue(string statusName)
    {
        // Arrange
        var status = InvitationStatus.FromName(statusName)!;

        // Assert
        status.IsFinal.Should().BeTrue();
    }

    #endregion

    #region CanBeResent

    [Fact]
    public void CanBeResent_Pending_ShouldReturnTrue()
    {
        // Assert
        InvitationStatus.Pending.CanBeResent.Should().BeTrue();
    }

    [Fact]
    public void CanBeResent_Expired_ShouldReturnTrue()
    {
        // Assert
        InvitationStatus.Expired.CanBeResent.Should().BeTrue();
    }

    [Theory]
    [InlineData("Accepted")]
    [InlineData("Declined")]
    [InlineData("Cancelled")]
    public void CanBeResent_FinalStatuses_ShouldReturnFalse(string statusName)
    {
        // Arrange
        var status = InvitationStatus.FromName(statusName)!;

        // Assert
        status.CanBeResent.Should().BeFalse();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameStatus_ShouldReturnTrue()
    {
        // Arrange
        var status1 = InvitationStatus.Pending;
        var status2 = InvitationStatus.FromId(1);

        // Assert
        (status1 == status2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentStatus_ShouldReturnFalse()
    {
        // Arrange
        var status1 = InvitationStatus.Pending;
        var status2 = InvitationStatus.Accepted;

        // Assert
        (status1 != status2).Should().BeTrue();
    }

    #endregion
}
