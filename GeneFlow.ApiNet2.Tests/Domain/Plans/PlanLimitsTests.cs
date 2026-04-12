using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Plans;

/// <summary>
/// Unit tests for the PlanLimits value object.
/// </summary>
public class PlanLimitsTests
{
    #region Create - Success

    [Fact]
    public void Create_WithValidLimits_ShouldReturnSuccess()
    {
        // Act
        var result = PlanLimits.Create(10, 500, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MaxStudies.Should().Be(10);
        result.Value.MaxTracesPerMonth.Should().Be(500);
        result.Value.MaxMembersPerStudy.Should().Be(10);
    }

    [Fact]
    public void Create_WithUnlimitedValues_ShouldReturnSuccess()
    {
        // Act
        var result = PlanLimits.Create(
            PlanLimits.Unlimited,
            PlanLimits.Unlimited,
            PlanLimits.Unlimited);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsUnlimitedStudies.Should().BeTrue();
        result.Value.IsUnlimitedTraces.Should().BeTrue();
        result.Value.IsUnlimitedMembers.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMixedLimits_ShouldReturnSuccess()
    {
        // Act
        var result = PlanLimits.Create(10, PlanLimits.Unlimited, 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsUnlimitedStudies.Should().BeFalse();
        result.Value.IsUnlimitedTraces.Should().BeTrue();
        result.Value.IsUnlimitedMembers.Should().BeFalse();
    }

    #endregion

    #region Create - Failures

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(-100)]
    public void Create_WithInvalidMaxStudies_ShouldReturnFailure(int maxStudies)
    {
        // Act
        var result = PlanLimits.Create(maxStudies, 100, 10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidMaxStudies");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(-100)]
    public void Create_WithInvalidMaxTraces_ShouldReturnFailure(int maxTraces)
    {
        // Act
        var result = PlanLimits.Create(10, maxTraces, 10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidMaxTraces");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(-100)]
    public void Create_WithInvalidMaxMembers_ShouldReturnFailure(int maxMembers)
    {
        // Act
        var result = PlanLimits.Create(10, 100, maxMembers);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidMaxMembers");
    }

    #endregion

    #region IsUnlimited Properties

    [Fact]
    public void IsUnlimitedStudies_WhenUnlimited_ShouldBeTrue()
    {
        // Arrange
        var limits = PlanLimits.Create(PlanLimits.Unlimited, 100, 10).Value;

        // Assert
        limits.IsUnlimitedStudies.Should().BeTrue();
    }

    [Fact]
    public void IsUnlimitedStudies_WhenLimited_ShouldBeFalse()
    {
        // Arrange
        var limits = PlanLimits.Create(10, 100, 10).Value;

        // Assert
        limits.IsUnlimitedStudies.Should().BeFalse();
    }

    [Fact]
    public void IsUnlimitedTraces_WhenUnlimited_ShouldBeTrue()
    {
        // Arrange
        var limits = PlanLimits.Create(10, PlanLimits.Unlimited, 10).Value;

        // Assert
        limits.IsUnlimitedTraces.Should().BeTrue();
    }

    [Fact]
    public void IsUnlimitedMembers_WhenUnlimited_ShouldBeTrue()
    {
        // Arrange
        var limits = PlanLimits.Create(10, 100, PlanLimits.Unlimited).Value;

        // Assert
        limits.IsUnlimitedMembers.Should().BeTrue();
    }

    #endregion

    #region Static Factory Methods

    [Fact]
    public void CreateUnlimited_ShouldReturnAllUnlimited()
    {
        // Act
        var limits = PlanLimits.CreateUnlimited();

        // Assert
        limits.MaxStudies.Should().Be(PlanLimits.Unlimited);
        limits.MaxTracesPerMonth.Should().Be(PlanLimits.Unlimited);
        limits.MaxMembersPerStudy.Should().Be(PlanLimits.Unlimited);
        limits.IsUnlimitedStudies.Should().BeTrue();
        limits.IsUnlimitedTraces.Should().BeTrue();
        limits.IsUnlimitedMembers.Should().BeTrue();
    }

    [Fact]
    public void FreeTier_ShouldReturnCorrectLimits()
    {
        // Act
        var limits = PlanLimits.FreeTier();

        // Assert
        limits.MaxStudies.Should().Be(2);
        limits.MaxTracesPerMonth.Should().Be(50);
        limits.MaxMembersPerStudy.Should().Be(3);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameLimits_ShouldBeEqual()
    {
        // Arrange
        var limits1 = PlanLimits.Create(10, 500, 10).Value;
        var limits2 = PlanLimits.Create(10, 500, 10).Value;

        // Assert
        limits1.Should().Be(limits2);
    }

    [Fact]
    public void Equals_WithDifferentLimits_ShouldNotBeEqual()
    {
        // Arrange
        var limits1 = PlanLimits.Create(10, 500, 10).Value;
        var limits2 = PlanLimits.Create(20, 1000, 20).Value;

        // Assert
        limits1.Should().NotBe(limits2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WithLimitedValues_ShouldShowNumbers()
    {
        // Arrange
        var limits = PlanLimits.Create(10, 500, 5).Value;

        // Act
        var result = limits.ToString();

        // Assert
        result.Should().Contain("10");
        result.Should().Contain("500");
        result.Should().Contain("5");
    }

    [Fact]
    public void ToString_WithUnlimitedValues_ShouldShowInfinitySymbol()
    {
        // Arrange
        var limits = PlanLimits.CreateUnlimited();

        // Act
        var result = limits.ToString();

        // Assert
        result.Should().Contain("∞");
    }

    #endregion
}
