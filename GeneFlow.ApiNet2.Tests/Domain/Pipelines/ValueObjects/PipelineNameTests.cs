using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.ValueObjects;

/// <summary>
/// Unit tests for the PipelineName value object.
/// </summary>
public class PipelineNameTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidName_ShouldReturnSuccess()
    {
        // Arrange
        const string name = "Quality Analysis Pipeline";

        // Act
        var result = PipelineName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(name);
    }

    [Fact]
    public void Create_WithMinLength_ShouldReturnSuccess()
    {
        // Arrange
        var minName = new string('a', PipelineName.MinLength);

        // Act
        var result = PipelineName.Create(minName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(PipelineName.MinLength);
    }

    [Fact]
    public void Create_WithMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var maxName = new string('a', PipelineName.MaxLength);

        // Act
        var result = PipelineName.Create(maxName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(PipelineName.MaxLength);
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        const string name = "  Pipeline Name  ";

        // Act
        var result = PipelineName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Pipeline Name");
    }

    #endregion

    #region Create - Invalid Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldReturnFailure(string? value)
    {
        // Act
        var result = PipelineName.Create(value!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Fact]
    public void Create_WithNameTooShort_ShouldReturnFailure()
    {
        // Arrange
        var shortName = new string('a', PipelineName.MinLength - 1);

        // Act
        var result = PipelineName.Create(shortName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longName = new string('a', PipelineName.MaxLength + 1);

        // Act
        var result = PipelineName.Create(longName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var name1 = PipelineName.Create("Test Pipeline").Value;
        var name2 = PipelineName.Create("Test Pipeline").Value;

        // Assert
        name1.Should().Be(name2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var name1 = PipelineName.Create("Pipeline One").Value;
        var name2 = PipelineName.Create("Pipeline Two").Value;

        // Assert
        name1.Should().NotBe(name2);
    }

    #endregion

    #region ToString and Implicit Conversion

    [Fact]
    public void ToString_ShouldReturnNameValue()
    {
        // Arrange
        var name = PipelineName.Create("Test Pipeline").Value;

        // Act
        var result = name.ToString();

        // Assert
        result.Should().Be("Test Pipeline");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnNameValue()
    {
        // Arrange
        var name = PipelineName.Create("Test Pipeline").Value;

        // Act
        string result = name;

        // Assert
        result.Should().Be("Test Pipeline");
    }

    #endregion
}
