using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Subscriptions;

/// <summary>
/// Unit tests for the SubscriptionStatus enumeration.
/// </summary>
public class SubscriptionStatusTests
{
    #region All Statuses

    [Fact]
    public void AllStatuses_ShouldHaveUniqueIds()
    {
        // Arrange
        var statuses = new[]
        {
            SubscriptionStatus.Active,
            SubscriptionStatus.Trial,
            SubscriptionStatus.PastDue,
            SubscriptionStatus.Cancelled,
            SubscriptionStatus.Expired,
            SubscriptionStatus.Suspended
        };

        // Assert
        var ids = statuses.Select(s => s.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllStatuses_ShouldHaveUniqueNames()
    {
        // Arrange
        var statuses = new[]
        {
            SubscriptionStatus.Active,
            SubscriptionStatus.Trial,
            SubscriptionStatus.PastDue,
            SubscriptionStatus.Cancelled,
            SubscriptionStatus.Expired,
            SubscriptionStatus.Suspended
        };

        // Assert
        var names = statuses.Select(s => s.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region GrantsAccess

    [Theory]
    [InlineData(1)] // Active
    [InlineData(2)] // Trial
    [InlineData(3)] // PastDue
    [InlineData(4)] // Cancelled
    public void GrantsAccess_ForAccessGrantingStatuses_ShouldBeTrue(int statusId)
    {
        // Arrange
        var status = SubscriptionStatus.FromId(statusId);

        // Assert
        status.Should().NotBeNull();
        status!.GrantsAccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(5)] // Expired
    [InlineData(6)] // Suspended
    public void GrantsAccess_ForNonAccessStatuses_ShouldBeFalse(int statusId)
    {
        // Arrange
        var status = SubscriptionStatus.FromId(statusId);

        // Assert
        status.Should().NotBeNull();
        status!.GrantsAccess.Should().BeFalse();
    }

    [Fact]
    public void Active_GrantsAccess_ShouldBeTrue()
    {
        // Assert
        SubscriptionStatus.Active.GrantsAccess.Should().BeTrue();
    }

    [Fact]
    public void Trial_GrantsAccess_ShouldBeTrue()
    {
        // Assert
        SubscriptionStatus.Trial.GrantsAccess.Should().BeTrue();
    }

    [Fact]
    public void PastDue_GrantsAccess_ShouldBeTrue()
    {
        // Assert
        SubscriptionStatus.PastDue.GrantsAccess.Should().BeTrue();
    }

    [Fact]
    public void Cancelled_GrantsAccess_ShouldBeTrue()
    {
        // Cancelled still grants access until period ends
        SubscriptionStatus.Cancelled.GrantsAccess.Should().BeTrue();
    }

    [Fact]
    public void Expired_GrantsAccess_ShouldBeFalse()
    {
        // Assert
        SubscriptionStatus.Expired.GrantsAccess.Should().BeFalse();
    }

    [Fact]
    public void Suspended_GrantsAccess_ShouldBeFalse()
    {
        // Assert
        SubscriptionStatus.Suspended.GrantsAccess.Should().BeFalse();
    }

    #endregion

    #region CanRenew

    [Theory]
    [InlineData(4)] // Cancelled
    [InlineData(5)] // Expired
    [InlineData(6)] // Suspended
    public void CanRenew_ForRenewableStatuses_ShouldBeTrue(int statusId)
    {
        // Arrange
        var status = SubscriptionStatus.FromId(statusId);

        // Assert
        status.Should().NotBeNull();
        status!.CanRenew.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)] // Active
    [InlineData(2)] // Trial
    [InlineData(3)] // PastDue
    public void CanRenew_ForNonRenewableStatuses_ShouldBeFalse(int statusId)
    {
        // Arrange
        var status = SubscriptionStatus.FromId(statusId);

        // Assert
        status.Should().NotBeNull();
        status!.CanRenew.Should().BeFalse();
    }

    #endregion

    #region CanCancel

    [Theory]
    [InlineData(1)] // Active
    [InlineData(2)] // Trial
    [InlineData(3)] // PastDue
    public void CanCancel_ForCancellableStatuses_ShouldBeTrue(int statusId)
    {
        // Arrange
        var status = SubscriptionStatus.FromId(statusId);

        // Assert
        status.Should().NotBeNull();
        status!.CanCancel.Should().BeTrue();
    }

    [Theory]
    [InlineData(4)] // Cancelled
    [InlineData(5)] // Expired
    [InlineData(6)] // Suspended
    public void CanCancel_ForNonCancellableStatuses_ShouldBeFalse(int statusId)
    {
        // Arrange
        var status = SubscriptionStatus.FromId(statusId);

        // Assert
        status.Should().NotBeNull();
        status!.CanCancel.Should().BeFalse();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Active")]
    [InlineData(2, "Trial")]
    [InlineData(3, "PastDue")]
    [InlineData(4, "Cancelled")]
    [InlineData(5, "Expired")]
    [InlineData(6, "Suspended")]
    public void FromId_WithValidId_ShouldReturnCorrectStatus(int id, string expectedName)
    {
        // Act
        var status = SubscriptionStatus.FromId(id);

        // Assert
        status.Should().NotBeNull();
        status!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var status = SubscriptionStatus.FromId(999);

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Active", 1)]
    [InlineData("Trial", 2)]
    [InlineData("PastDue", 3)]
    [InlineData("Cancelled", 4)]
    [InlineData("Expired", 5)]
    [InlineData("Suspended", 6)]
    public void FromName_WithValidName_ShouldReturnCorrectStatus(string name, int expectedId)
    {
        // Act
        var status = SubscriptionStatus.FromName(name);

        // Assert
        status.Should().NotBeNull();
        status!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var status = SubscriptionStatus.FromName("Invalid");

        // Assert
        status.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameStatus_ShouldBeTrue()
    {
        // Arrange
        var status1 = SubscriptionStatus.Active;
        var status2 = SubscriptionStatus.Active;

        // Assert
        status1.Should().Be(status2);
        (status1 == status2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentStatus_ShouldBeFalse()
    {
        // Arrange
        var status1 = SubscriptionStatus.Active;
        var status2 = SubscriptionStatus.Expired;

        // Assert
        status1.Should().NotBe(status2);
        (status1 != status2).Should().BeTrue();
    }

    #endregion
}
