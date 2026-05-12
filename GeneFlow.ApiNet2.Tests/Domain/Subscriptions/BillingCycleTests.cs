using GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Subscriptions;

/// <summary>
/// Unit tests for the BillingCycle enumeration.
/// </summary>
public class BillingCycleTests
{
    #region All Billing Cycles

    [Fact]
    public void AllBillingCycles_ShouldHaveUniqueIds()
    {
        // Arrange
        var billingCycles = new[]
        {
            BillingCycle.Monthly,
            BillingCycle.Annual
        };

        // Assert
        var ids = billingCycles.Select(bc => bc.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllBillingCycles_ShouldHaveUniqueNames()
    {
        // Arrange
        var billingCycles = new[]
        {
            BillingCycle.Monthly,
            BillingCycle.Annual
        };

        // Assert
        var names = billingCycles.Select(bc => bc.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Months Property

    [Fact]
    public void Monthly_Months_ShouldReturn1()
    {
        // Assert
        BillingCycle.Monthly.Months.Should().Be(1);
    }

    [Fact]
    public void Annual_Months_ShouldReturn12()
    {
        // Assert
        BillingCycle.Annual.Months.Should().Be(12);
    }

    #endregion

    #region CalculateNextBillingDate

    [Fact]
    public void CalculateNextBillingDate_Monthly_ShouldAddOneMonth()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var nextDate = BillingCycle.Monthly.CalculateNextBillingDate(startDate);

        // Assert
        nextDate.Should().Be(new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateNextBillingDate_Annual_ShouldAddTwelveMonths()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var nextDate = BillingCycle.Annual.CalculateNextBillingDate(startDate);

        // Assert
        nextDate.Should().Be(new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateNextBillingDate_MonthlyFromEndOfMonth_ShouldHandleCorrectly()
    {
        // Arrange - Jan 31 + 1 month = Feb 28/29
        var startDate = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var nextDate = BillingCycle.Monthly.CalculateNextBillingDate(startDate);

        // Assert - 2024 is a leap year, so Feb has 29 days
        nextDate.Should().Be(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CalculateNextBillingDate_AnnualFromLeapDay_ShouldHandleCorrectly()
    {
        // Arrange - Feb 29, 2024 (leap year)
        var startDate = new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var nextDate = BillingCycle.Annual.CalculateNextBillingDate(startDate);

        // Assert - 2025 is not a leap year, so Feb 28
        nextDate.Should().Be(new DateTime(2025, 2, 28, 0, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Monthly")]
    [InlineData(2, "Annual")]
    public void FromId_WithValidId_ShouldReturnCorrectBillingCycle(int id, string expectedName)
    {
        // Act
        var billingCycle = BillingCycle.FromId(id);

        // Assert
        billingCycle.Should().NotBeNull();
        billingCycle!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var billingCycle = BillingCycle.FromId(999);

        // Assert
        billingCycle.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Monthly", 1)]
    [InlineData("Annual", 2)]
    public void FromName_WithValidName_ShouldReturnCorrectBillingCycle(string name, int expectedId)
    {
        // Act
        var billingCycle = BillingCycle.FromName(name);

        // Assert
        billingCycle.Should().NotBeNull();
        billingCycle!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var billingCycle = BillingCycle.FromName("Invalid");

        // Assert
        billingCycle.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameBillingCycle_ShouldBeTrue()
    {
        // Arrange
        var billingCycle1 = BillingCycle.Monthly;
        var billingCycle2 = BillingCycle.Monthly;

        // Assert
        billingCycle1.Should().Be(billingCycle2);
        (billingCycle1 == billingCycle2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentBillingCycle_ShouldBeFalse()
    {
        // Arrange
        var billingCycle1 = BillingCycle.Monthly;
        var billingCycle2 = BillingCycle.Annual;

        // Assert
        billingCycle1.Should().NotBe(billingCycle2);
        (billingCycle1 != billingCycle2).Should().BeTrue();
    }

    #endregion
}
