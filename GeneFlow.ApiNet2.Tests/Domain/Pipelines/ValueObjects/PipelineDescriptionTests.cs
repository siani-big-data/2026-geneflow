using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.ValueObjects;

/// <summary>
/// Unit tests for the PipelineDescription value object.
/// </summary>
public class PipelineDescriptionTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidDescription_ShouldReturnSuccess()
    {
        // Arrange
        const string description = "This pipeline performs quality analysis on DNA sequences.";

        // Act
        var result = PipelineDescription.Create(description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(description);
    }

    [Fact]
    public void Create_WithNull_ShouldReturnEmptyDescription()
    {
        // Act
        var result = PipelineDescription.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmpty_ShouldReturnEmptyDescription()
    {
        // Act
        var result = PipelineDescription.Create("");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithWhitespace_ShouldReturnEmptyDescription()
    {
        // Act
        var result = PipelineDescription.Create("   ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_WithMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var maxDescription = new string('a', PipelineDescription.MaxLength);

        // Act
        var result = PipelineDescription.Create(maxDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(PipelineDescription.MaxLength);
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        const string description = "  Description text  ";

        // Act
        var result = PipelineDescription.Create(description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Description text");
    }

    #endregion

    #region Create - Invalid Cases

    [Fact]
    public void Create_WithDescriptionTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longDescription = new string('a', PipelineDescription.MaxLength + 1);

        // Act
        var result = PipelineDescription.Create(longDescription);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Description");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldHaveNullValue()
    {
        // Act
        var empty = PipelineDescription.Empty;

        // Assert
        empty.Value.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var desc1 = PipelineDescription.Create("Test description").Value;
        var desc2 = PipelineDescription.Create("Test description").Value;

        // Assert
        desc1.Should().Be(desc2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var desc1 = PipelineDescription.Create("Description one").Value;
        var desc2 = PipelineDescription.Create("Description two").Value;

        // Assert
        desc1.Should().NotBe(desc2);
    }

    [Fact]
    public void Equals_BothEmpty_ShouldReturnTrue()
    {
        // Arrange
        var desc1 = PipelineDescription.Create(null).Value;
        var desc2 = PipelineDescription.Empty;

        // Assert
        desc1.Should().Be(desc2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WithValue_ShouldReturnDescriptionValue()
    {
        // Arrange
        var desc = PipelineDescription.Create("Test description").Value;

        // Act
        var result = desc.ToString();

        // Assert
        result.Should().Be("Test description");
    }

    [Fact]
    public void ToString_WhenEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var desc = PipelineDescription.Empty;

        // Act
        var result = desc.ToString();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ShouldReturnDescriptionValue()
    {
        // Arrange
        var desc = PipelineDescription.Create("Test description").Value;

        // Act
        string? result = desc;

        // Assert
        result.Should().Be("Test description");
    }

    [Fact]
    public void ImplicitConversion_WhenEmpty_ShouldReturnNull()
    {
        // Arrange
        var desc = PipelineDescription.Empty;

        // Act
        string? result = desc;

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
