using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.ValueObjects;

/// <summary>
/// Unit tests for TraceDescription value object.
/// </summary>
public class TraceDescriptionTests
{
    #region Create - Success Cases

    [Fact]
    public void Create_WithValidDescription_ShouldSucceed()
    {
        // Arrange
        var description = "Forward sequencing trace for BRCA1 gene analysis";

        // Act
        var result = TraceDescription.Create(description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(description);
    }

    [Fact]
    public void Create_WithNull_ShouldCreateEmptyDescription()
    {
        // Act
        var result = TraceDescription.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyString_ShouldCreateEmptyDescription()
    {
        // Act
        var result = TraceDescription.Create(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithOnlyWhitespace_ShouldCreateEmptyDescription()
    {
        // Act
        var result = TraceDescription.Create("   ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithMaximumLength_ShouldSucceed()
    {
        // Arrange
        var description = new string('A', TraceDescription.MaxLength);

        // Act
        var result = TraceDescription.Create(description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(description);
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimAndSucceed()
    {
        // Arrange
        var description = "  Description with whitespace  ";

        // Act
        var result = TraceDescription.Create(description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Description with whitespace");
    }

    #endregion

    #region Create - Failure Cases

    [Fact]
    public void Create_WithDescriptionTooLong_ShouldFail()
    {
        // Arrange
        var description = new string('A', TraceDescription.MaxLength + 1);

        // Act
        var result = TraceDescription.Create(description);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("DescriptionTooLong");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldReturnDescriptionWithNullValue()
    {
        // Act
        var empty = TraceDescription.Empty;

        // Assert
        empty.Value.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameDescription_ShouldBeEqual()
    {
        // Arrange
        var desc1 = TraceDescription.Create("Same description").Value;
        var desc2 = TraceDescription.Create("Same description").Value;

        // Act & Assert
        desc1.Should().Be(desc2);
    }

    [Fact]
    public void Equals_WithDifferentDescriptions_ShouldNotBeEqual()
    {
        // Arrange
        var desc1 = TraceDescription.Create("First description").Value;
        var desc2 = TraceDescription.Create("Second description").Value;

        // Act & Assert
        desc1.Should().NotBe(desc2);
    }

    [Fact]
    public void Equals_EmptyDescriptions_ShouldBeEqual()
    {
        // Arrange
        var desc1 = TraceDescription.Empty;
        var desc2 = TraceDescription.Create(null).Value;

        // Act & Assert
        desc1.Should().Be(desc2);
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var description = TraceDescription.Create("Test description").Value;

        // Act
        string? result = description;

        // Assert
        result.Should().Be("Test description");
    }

    [Fact]
    public void ImplicitConversion_EmptyToString_ShouldReturnNull()
    {
        // Arrange
        var description = TraceDescription.Empty;

        // Act
        string? result = description;

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WithValue_ShouldReturnValue()
    {
        // Arrange
        var description = TraceDescription.Create("Test description").Value;

        // Act
        var result = description.ToString();

        // Assert
        result.Should().Be("Test description");
    }

    [Fact]
    public void ToString_Empty_ShouldReturnEmptyString()
    {
        // Arrange
        var description = TraceDescription.Empty;

        // Act
        var result = description.ToString();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Constants

    [Fact]
    public void MaxLength_ShouldBeFiveHundred()
    {
        TraceDescription.MaxLength.Should().Be(500);
    }

    #endregion
}
