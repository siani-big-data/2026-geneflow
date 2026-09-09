using GeneFlow.ApiNet2.Domain.Plans;

namespace GeneFlow.ApiNet2.Tests.Domain.Plans;

/// <summary>
/// Unit tests for the PlanId strongly-typed identifier.
/// </summary>
public class PlanIdTests
{
    #region Constructor

    [Fact]
    public void Constructor_ShouldCreateValidPlanId()
    {
        // Act
        var planId = new PlanId(1);

        // Assert
        planId.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithZero_ShouldCreatePlanId()
    {
        // Act
        var planId = new PlanId(0);

        // Assert
        planId.Should().NotBeNull();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnFormattedId()
    {
        // Arrange
        var planId = new PlanId(1);

        // Act
        var result = planId.ToString();

        // Assert
        result.Should().Be("L00000001");
    }

    [Fact]
    public void ToString_WithLargerNumber_ShouldPadCorrectly()
    {
        // Arrange
        var planId = new PlanId(12345);

        // Act
        var result = planId.ToString();

        // Assert
        result.Should().Be("L00012345");
    }

    [Fact]
    public void ToString_WithMaxDigits_ShouldNotPad()
    {
        // Arrange
        var planId = new PlanId(12345678);

        // Act
        var result = planId.ToString();

        // Assert
        result.Should().Be("L12345678");
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_WithValidId_ShouldReturnPlanId()
    {
        // Arrange
        const string id = "L00000001";

        // Act
        var planId = PlanId.Parse(id);

        // Assert
        planId.Should().NotBeNull();
        planId.ToString().Should().Be(id);
    }

    [Fact]
    public void Parse_WithLargerNumber_ShouldReturnCorrectPlanId()
    {
        // Arrange
        const string id = "L00012345";

        // Act
        var planId = PlanId.Parse(id);

        // Assert
        planId.ToString().Should().Be(id);
    }

    [Fact]
    public void Parse_WithInvalidPrefix_ShouldThrow()
    {
        // Arrange
        const string id = "X00000001";

        // Act & Assert
        var act = () => PlanId.Parse(id);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_WithInvalidFormat_ShouldThrow()
    {
        // Arrange
        const string id = "L123";

        // Act & Assert
        var act = () => PlanId.Parse(id);
        act.Should().Throw<Exception>();
    }

    #endregion

    #region TryParse

    [Fact]
    public void TryParse_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        const string id = "L00000001";

        // Act
        var success = PlanId.TryParse(id, out var planId);

        // Assert
        success.Should().BeTrue();
        planId.Should().NotBeNull();
        planId!.ToString().Should().Be(id);
    }

    [Fact]
    public void TryParse_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        const string id = "invalid";

        // Act
        var success = PlanId.TryParse(id, out var planId);

        // Assert
        success.Should().BeFalse();
        planId.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithNull_ShouldReturnFalse()
    {
        // Act
        var success = PlanId.TryParse(null, out var planId);

        // Assert
        success.Should().BeFalse();
        planId.Should().BeNull();
    }

    #endregion

    #region FromSequence

    [Fact]
    public void FromSequence_ShouldCreatePlanId()
    {
        // Arrange
        const long sequenceValue = 42;

        // Act
        var planId = PlanId.FromSequence(sequenceValue);

        // Assert
        planId.ToString().Should().Be("L00000042");
    }

    #endregion

    #region SequenceName

    [Fact]
    public void SequenceName_ShouldBePlans()
    {
        PlanId.SequenceName.Should().Be("plans");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var planId1 = new PlanId(1);
        var planId2 = new PlanId(1);

        // Assert
        planId1.Should().Be(planId2);
        (planId1 == planId2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var planId1 = new PlanId(1);
        var planId2 = new PlanId(2);

        // Assert
        planId1.Should().NotBe(planId2);
        (planId1 != planId2).Should().BeTrue();
    }

    #endregion
}
