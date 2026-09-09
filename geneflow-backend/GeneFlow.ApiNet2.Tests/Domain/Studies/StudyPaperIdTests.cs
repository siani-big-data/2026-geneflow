using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies;

/// <summary>
/// Unit tests for StudyPaperId strongly-typed identifier.
/// </summary>
public class StudyPaperIdTests
{
    #region Constructor

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(99999999)]
    public void Constructor_WithValidValue_ShouldCreateId(long value)
    {
        // Act
        var paperId = new StudyPaperId(value);

        // Assert
        paperId.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_WithZero_ShouldBeAllowed()
    {
        // Act
        var paperId = new StudyPaperId(0);

        // Assert
        paperId.Value.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNegativeValue_ShouldThrowArgumentException(long value)
    {
        // Act
        var action = () => new StudyPaperId(value);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnPrefixedFormat()
    {
        // Arrange
        var paperId = new StudyPaperId(1);

        // Act
        var result = paperId.ToString();

        // Assert
        result.Should().Be("R00000001");
    }

    [Fact]
    public void ToString_WithLargeValue_ShouldPadCorrectly()
    {
        // Arrange
        var paperId = new StudyPaperId(12345678);

        // Act
        var result = paperId.ToString();

        // Assert
        result.Should().Be("R12345678");
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_WithValidString_ShouldReturnStudyPaperId()
    {
        // Arrange
        const string value = "R00000001";

        // Act
        var paperId = StudyPaperId.Parse(value);

        // Assert
        paperId.Value.Should().Be(1);
    }

    [Fact]
    public void Parse_WithLargeNumber_ShouldReturnCorrectValue()
    {
        // Arrange
        const string value = "R12345678";

        // Act
        var paperId = StudyPaperId.Parse(value);

        // Assert
        paperId.Value.Should().Be(12345678);
    }

    [Theory]
    [InlineData("")]
    [InlineData("R")]
    [InlineData("S00000001")]
    [InlineData("R0000000A")]
    public void Parse_WithInvalidString_ShouldThrowException(string value)
    {
        // Act
        var action = () => StudyPaperId.Parse(value);

        // Assert
        action.Should().Throw<Exception>();
    }

    #endregion

    #region TryParse

    [Fact]
    public void TryParse_WithValidString_ShouldReturnTrueAndPaperId()
    {
        // Arrange
        const string value = "R00000001";

        // Act
        var success = StudyPaperId.TryParse(value, out var paperId);

        // Assert
        success.Should().BeTrue();
        paperId.Should().NotBeNull();
        paperId!.Value.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("R")]
    [InlineData("S00000001")]
    public void TryParse_WithInvalidString_ShouldReturnFalse(string value)
    {
        // Act
        var success = StudyPaperId.TryParse(value, out var paperId);

        // Assert
        success.Should().BeFalse();
        paperId.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var id1 = new StudyPaperId(1);
        var id2 = new StudyPaperId(1);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var id1 = new StudyPaperId(1);
        var id2 = new StudyPaperId(2);

        // Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var id1 = new StudyPaperId(123);
        var id2 = new StudyPaperId(123);

        // Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    #endregion
}
