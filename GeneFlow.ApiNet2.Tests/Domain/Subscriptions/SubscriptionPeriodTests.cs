using GeneFlow.ApiNet2.Domain.Subscriptions.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Subscriptions;

/// <summary>
/// Unit tests for the SubscriptionPeriod value object.
/// </summary>
public class SubscriptionPeriodTests
{
    #region Create - Success

    [Fact]
    public void Create_WithValidDates_ShouldReturnSuccess()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddMonths(1);

        // Act
        var result = SubscriptionPeriod.Create(startDate, endDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(startDate);
        result.Value.EndDate.Should().Be(endDate);
    }

    #endregion

    #region Create - Failures

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ShouldReturnFailure()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(-1);

        // Act
        var result = SubscriptionPeriod.Create(startDate, endDate);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPeriod");
    }

    [Fact]
    public void Create_WithSameStartAndEndDate_ShouldReturnFailure()
    {
        // Arrange
        var date = DateTime.UtcNow;

        // Act
        var result = SubscriptionPeriod.Create(date, date);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPeriod");
    }

    #endregion

    #region CreateFromNow

    [Fact]
    public void CreateFromNow_ShouldCreatePeriodStartingNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var period = SubscriptionPeriod.CreateFromNow(1);
        var afterCreation = DateTime.UtcNow;

        // Assert
        period.StartDate.Should().BeOnOrAfter(beforeCreation);
        period.StartDate.Should().BeOnOrBefore(afterCreation);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(24)]
    public void CreateFromNow_ShouldCreateCorrectEndDate(int months)
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var period = SubscriptionPeriod.CreateFromNow(months);

        // Assert
        var expectedEnd = now.AddMonths(months);
        period.EndDate.Should().BeCloseTo(expectedEnd, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region CreateFromDate

    [Fact]
    public void CreateFromDate_ShouldCreatePeriodFromSpecifiedDate()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var period = SubscriptionPeriod.CreateFromDate(startDate, 12);

        // Assert
        period.StartDate.Should().Be(startDate);
        period.EndDate.Should().Be(new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    #endregion

    #region IsActive

    [Fact]
    public void IsActive_WhenWithinPeriod_ShouldBeTrue()
    {
        // Arrange
        var period = SubscriptionPeriod.CreateFromNow(1);

        // Assert
        period.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenBeforeStartDate_ShouldBeFalse()
    {
        // Arrange
        var futureStart = DateTime.UtcNow.AddDays(10);
        var futureEnd = futureStart.AddMonths(1);
        var period = SubscriptionPeriod.Create(futureStart, futureEnd).Value;

        // Assert
        period.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenAfterEndDate_ShouldBeFalse()
    {
        // Arrange
        var pastStart = DateTime.UtcNow.AddMonths(-2);
        var pastEnd = pastStart.AddMonths(1);
        var period = SubscriptionPeriod.Create(pastStart, pastEnd).Value;

        // Assert
        period.IsActive.Should().BeFalse();
    }

    #endregion

    #region HasExpired

    [Fact]
    public void HasExpired_WhenPeriodIsActive_ShouldBeFalse()
    {
        // Arrange
        var period = SubscriptionPeriod.CreateFromNow(1);

        // Assert
        period.HasExpired.Should().BeFalse();
    }

    [Fact]
    public void HasExpired_WhenPeriodHasPassed_ShouldBeTrue()
    {
        // Arrange
        var pastStart = DateTime.UtcNow.AddMonths(-2);
        var pastEnd = DateTime.UtcNow.AddMonths(-1);
        var period = SubscriptionPeriod.Create(pastStart, pastEnd).Value;

        // Assert
        period.HasExpired.Should().BeTrue();
    }

    #endregion

    #region DaysRemaining

    [Fact]
    public void DaysRemaining_WhenActive_ShouldReturnPositiveValue()
    {
        // Arrange
        var period = SubscriptionPeriod.CreateFromNow(1);

        // Assert
        period.DaysRemaining.Should().BeGreaterThan(0);
        period.DaysRemaining.Should().BeLessThanOrEqualTo(31);
    }

    [Fact]
    public void DaysRemaining_WhenExpired_ShouldReturnZero()
    {
        // Arrange
        var pastStart = DateTime.UtcNow.AddMonths(-2);
        var pastEnd = DateTime.UtcNow.AddMonths(-1);
        var period = SubscriptionPeriod.Create(pastStart, pastEnd).Value;

        // Assert
        period.DaysRemaining.Should().Be(0);
    }

    #endregion

    #region Duration

    [Fact]
    public void Duration_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var period = SubscriptionPeriod.Create(startDate, endDate).Value;

        // Assert
        period.Duration.Should().Be(TimeSpan.FromDays(31));
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameDates_ShouldBeEqual()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var period1 = SubscriptionPeriod.Create(startDate, endDate).Value;
        var period2 = SubscriptionPeriod.Create(startDate, endDate).Value;

        // Assert
        period1.Should().Be(period2);
    }

    [Fact]
    public void Equals_WithDifferentDates_ShouldNotBeEqual()
    {
        // Arrange
        var period1 = SubscriptionPeriod.CreateFromDate(DateTime.UtcNow, 1);
        var period2 = SubscriptionPeriod.CreateFromDate(DateTime.UtcNow.AddDays(1), 1);

        // Assert
        period1.Should().NotBe(period2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnFormattedDateRange()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var period = SubscriptionPeriod.Create(startDate, endDate).Value;

        // Act
        var result = period.ToString();

        // Assert
        result.Should().Contain("2024-01-15");
        result.Should().Contain("2024-02-15");
    }

    #endregion
}
