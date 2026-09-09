using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.ValueObjects;

/// <summary>
/// Unit tests for the Location value object.
/// </summary>
public class LocationTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidValue_ShouldCreate()
    {
        // Arrange
        var value = "New York, USA";

        // Act
        var result = Location.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("New York, USA");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var value = "  Boston, MA  ";

        // Act
        var result = Location.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Boston, MA");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_ShouldReturnEmptyLocation(string? value)
    {
        // Act
        var result = Location.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithCityAndCountry_ShouldCreate()
    {
        // Arrange
        var value = "Cambridge, Massachusetts, United States";

        // Act
        var result = Location.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Cambridge, Massachusetts, United States");
    }

    #endregion

    #region Create - Validation Failures

    [Fact]
    public void Create_TooShort_ShouldReturnError()
    {
        // Arrange
        var value = "A"; // Less than MinLength (2)

        // Act
        var result = Location.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("LocationTooShort");
    }

    [Fact]
    public void Create_TooLong_ShouldReturnError()
    {
        // Arrange
        var value = new string('A', Location.MaxLength + 1);

        // Act
        var result = Location.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("LocationTooLong");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldReturnNullValue()
    {
        // Act
        var empty = Location.Empty;

        // Assert
        empty.Value.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_SameLocation_ShouldBeEqual()
    {
        // Arrange
        var location1 = Location.Create("New York").Value;
        var location2 = Location.Create("New York").Value;

        // Assert
        location1.Should().Be(location2);
        (location1 == location2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentLocation_ShouldNotBeEqual()
    {
        // Arrange
        var location1 = Location.Create("New York").Value;
        var location2 = Location.Create("Boston").Value;

        // Assert
        location1.Should().NotBe(location2);
    }

    [Fact]
    public void Equality_EmptyLocations_ShouldBeEqual()
    {
        // Arrange
        var location1 = Location.Empty;
        var location2 = Location.Create(null).Value;

        // Assert
        location1.Should().Be(location2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WithValue_ShouldReturnValue()
    {
        // Arrange
        var location = Location.Create("Cambridge, MA").Value;

        // Assert
        location.ToString().Should().Be("Cambridge, MA");
    }

    [Fact]
    public void ToString_Empty_ShouldReturnEmptyString()
    {
        // Arrange
        var empty = Location.Empty;

        // Assert
        empty.ToString().Should().BeEmpty();
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ShouldConvertToString()
    {
        // Arrange
        var location = Location.Create("Seattle, WA").Value;

        // Act
        string? value = location;

        // Assert
        value.Should().Be("Seattle, WA");
    }

    [Fact]
    public void ImplicitConversion_Empty_ShouldReturnNull()
    {
        // Arrange
        var empty = Location.Empty;

        // Act
        string? value = empty;

        // Assert
        value.Should().BeNull();
    }

    #endregion

    #region Constants

    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        // Assert
        Location.MinLength.Should().Be(2);
        Location.MaxLength.Should().Be(200);
    }

    #endregion
}
