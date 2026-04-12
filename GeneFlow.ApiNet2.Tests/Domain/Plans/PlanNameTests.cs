using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Plans;

/// <summary>
/// Unit tests for the PlanName value object.
/// </summary>
public class PlanNameTests
{
    #region Create - Success

    [Theory]
    [InlineData("Free")]
    [InlineData("Pro")]
    [InlineData("Enterprise")]
    [InlineData("Basic Plan")]
    public void Create_WithValidName_ShouldReturnSuccess(string name)
    {
        // Act
        var result = PlanName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(name);
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var nameWithSpaces = "  Pro Plan  ";

        // Act
        var result = PlanName.Create(nameWithSpaces);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Pro Plan");
    }

    [Fact]
    public void Create_WithMinLength_ShouldSucceed()
    {
        // Arrange
        var name = new string('a', PlanName.MinLength);

        // Act
        var result = PlanName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMaxLength_ShouldSucceed()
    {
        // Arrange
        var name = new string('a', PlanName.MaxLength);

        // Act
        var result = PlanName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Create - Failures

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrNull_ShouldReturnFailure(string? name)
    {
        // Act
        var result = PlanName.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameRequired");
    }

    [Fact]
    public void Create_WithTooShortName_ShouldReturnFailure()
    {
        // Arrange
        var name = new string('a', PlanName.MinLength - 1);

        // Act
        var result = PlanName.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameTooShort");
    }

    [Fact]
    public void Create_WithTooLongName_ShouldReturnFailure()
    {
        // Arrange
        var name = new string('a', PlanName.MaxLength + 1);

        // Act
        var result = PlanName.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameTooLong");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameName_ShouldBeEqual()
    {
        // Arrange
        var name1 = PlanName.Create("Pro").Value;
        var name2 = PlanName.Create("Pro").Value;

        // Assert
        name1.Should().Be(name2);
        (name1 == name2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentNames_ShouldNotBeEqual()
    {
        // Arrange
        var name1 = PlanName.Create("Pro").Value;
        var name2 = PlanName.Create("Free").Value;

        // Assert
        name1.Should().NotBe(name2);
        (name1 != name2).Should().BeTrue();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var name = PlanName.Create("Enterprise").Value;

        // Act
        var result = name.ToString();

        // Assert
        result.Should().Be("Enterprise");
    }

    #endregion
}
