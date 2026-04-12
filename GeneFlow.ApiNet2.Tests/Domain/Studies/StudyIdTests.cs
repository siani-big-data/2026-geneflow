using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies;

/// <summary>
/// Unit tests for StudyId strongly-typed identifier.
/// </summary>
public class StudyIdTests
{
    #region Constructor

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(99999999)]
    public void Constructor_WithValidValue_ShouldCreateId(long value)
    {
        // Act
        var studyId = new StudyId(value);

        // Assert
        studyId.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNegativeValue_ShouldThrowArgumentException(long value)
    {
        // Act
        var action = () => new StudyId(value);

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithZero_ShouldBeAllowed()
    {
        // Act
        var studyId = new StudyId(0);

        // Assert
        studyId.Value.Should().Be(0);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnPrefixedFormat()
    {
        // Arrange
        var studyId = new StudyId(1);

        // Act
        var result = studyId.ToString();

        // Assert
        result.Should().Be("S00000001");
    }

    [Fact]
    public void ToString_WithLargeValue_ShouldPadCorrectly()
    {
        // Arrange
        var studyId = new StudyId(12345678);

        // Act
        var result = studyId.ToString();

        // Assert
        result.Should().Be("S12345678");
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_WithValidString_ShouldReturnStudyId()
    {
        // Arrange
        const string value = "S00000001";

        // Act
        var studyId = StudyId.Parse(value);

        // Assert
        studyId.Value.Should().Be(1);
    }

    [Fact]
    public void Parse_WithLargeNumber_ShouldReturnCorrectValue()
    {
        // Arrange
        const string value = "S12345678";

        // Act
        var studyId = StudyId.Parse(value);

        // Assert
        studyId.Value.Should().Be(12345678);
    }

    [Theory]
    [InlineData("")]
    [InlineData("S")]
    [InlineData("X00000001")]
    [InlineData("S0000000A")]
    public void Parse_WithInvalidString_ShouldThrowException(string value)
    {
        // Act
        var action = () => StudyId.Parse(value);

        // Assert
        action.Should().Throw<Exception>();
    }

    #endregion

    #region TryParse

    [Fact]
    public void TryParse_WithValidString_ShouldReturnTrueAndStudyId()
    {
        // Arrange
        const string value = "S00000001";

        // Act
        var success = StudyId.TryParse(value, out var studyId);

        // Assert
        success.Should().BeTrue();
        studyId.Should().NotBeNull();
        studyId!.Value.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("S")]
    [InlineData("X00000001")]
    public void TryParse_WithInvalidString_ShouldReturnFalse(string value)
    {
        // Act
        var success = StudyId.TryParse(value, out var studyId);

        // Assert
        success.Should().BeFalse();
        studyId.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var id1 = new StudyId(1);
        var id2 = new StudyId(1);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var id1 = new StudyId(1);
        var id2 = new StudyId(2);

        // Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var id1 = new StudyId(123);
        var id2 = new StudyId(123);

        // Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    #endregion
}
