using GeneFlow.ApiNet2.Domain.Pipelines;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines;

/// <summary>
/// Unit tests for PipelineId strongly-typed identifier.
/// </summary>
public class PipelineIdTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithValidValue_ShouldCreate()
    {
        // Arrange
        const long value = 12345678;

        // Act
        var id = new PipelineId(value);

        // Assert
        id.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99999999)]
    [InlineData(12345678)]
    public void Constructor_WithDifferentValues_ShouldCreate(long value)
    {
        // Act
        var id = new PipelineId(value);

        // Assert
        id.Value.Should().Be(value);
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_WithValidFormat_ShouldReturnPipelineId()
    {
        // Arrange
        const string idString = "P00012345";

        // Act
        var id = PipelineId.Parse(idString);

        // Assert
        id.Should().NotBeNull();
        id.Value.Should().Be(12345);
    }

    [Fact]
    public void Parse_WithDifferentValues_ShouldParseCorrectly()
    {
        // Arrange
        var id1 = new PipelineId(1);
        var id2 = new PipelineId(99999999);

        // Act & Assert
        PipelineId.Parse(id1.ToString()).Value.Should().Be(1);
        PipelineId.Parse(id2.ToString()).Value.Should().Be(99999999);
    }

    #endregion

    #region TryParse

    [Fact]
    public void TryParse_WithValidFormat_ShouldReturnTrueAndPipelineId()
    {
        // Arrange
        const string idString = "P00012345";

        // Act
        var success = PipelineId.TryParse(idString, out var id);

        // Assert
        success.Should().BeTrue();
        id.Should().NotBeNull();
        id!.Value.Should().Be(12345);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("S00012345")] // Wrong prefix
    [InlineData("P123")]      // Too short
    public void TryParse_WithInvalidFormat_ShouldReturnFalse(string? idString)
    {
        // Act
        var success = PipelineId.TryParse(idString, out var id);

        // Assert
        success.Should().BeFalse();
        id.Should().BeNull();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnPrefixedFormat()
    {
        // Arrange
        var id = new PipelineId(12345);

        // Act
        var result = id.ToString();

        // Assert
        result.Should().StartWith("P");
        result.Should().HaveLength(9); // P + 8 digits
    }

    [Fact]
    public void ToString_ShouldPadWithZeros()
    {
        // Arrange
        var id = new PipelineId(1);

        // Act
        var result = id.ToString();

        // Assert
        result.Should().Be("P00000001");
    }

    #endregion

    #region FromSequence

    [Fact]
    public void FromSequence_ShouldCreatePipelineId()
    {
        // Arrange
        const long sequenceValue = 42;

        // Act
        var id = PipelineId.FromSequence(sequenceValue);

        // Assert
        id.Value.Should().Be(sequenceValue);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameValue_ShouldReturnTrue()
    {
        // Arrange
        var id1 = new PipelineId(12345);
        var id2 = new PipelineId(12345);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var id1 = new PipelineId(12345);
        var id2 = new PipelineId(67890);

        // Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameValue_ShouldBeSame()
    {
        // Arrange
        var id1 = new PipelineId(12345);
        var id2 = new PipelineId(12345);

        // Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    #endregion

    #region SequenceName

    [Fact]
    public void SequenceName_ShouldBePipelines()
    {
        // Assert
        PipelineId.SequenceName.Should().Be("pipelines");
    }

    #endregion
}
