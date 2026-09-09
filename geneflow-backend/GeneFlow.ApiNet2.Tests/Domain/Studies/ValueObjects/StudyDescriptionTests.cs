using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.ValueObjects;

/// <summary>
/// Unit tests for the StudyDescription value object.
/// </summary>
public class StudyDescriptionTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidDescription_ShouldReturnSuccess()
    {
        // Arrange
        const string description = "This study analyzes genetic mutations in cancer patients.";

        // Act
        var result = StudyDescription.Create(description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldReturnSuccessWithNullValue(string? value)
    {
        // Act
        var result = StudyDescription.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        const string description = "  Some description text  ";

        // Act
        var result = StudyDescription.Create(description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Some description text");
    }

    [Fact]
    public void Create_WithMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var maxDescription = new string('a', StudyDescription.MaxLength);

        // Act
        var result = StudyDescription.Create(maxDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(StudyDescription.MaxLength);
    }

    #endregion

    #region Create - Invalid Cases

    [Fact]
    public void Create_WithDescriptionTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longDescription = new string('a', StudyDescription.MaxLength + 1);

        // Act
        var result = StudyDescription.Create(longDescription);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Description");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldReturnDescriptionWithNullValue()
    {
        // Act
        var description = StudyDescription.Empty;

        // Assert
        description.Value.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var desc1 = StudyDescription.Create("Test description").Value;
        var desc2 = StudyDescription.Create("Test description").Value;

        // Assert
        desc1.Should().Be(desc2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var desc1 = StudyDescription.Create("Description one").Value;
        var desc2 = StudyDescription.Create("Description two").Value;

        // Assert
        desc1.Should().NotBe(desc2);
    }

    [Fact]
    public void Equals_BothEmpty_ShouldReturnTrue()
    {
        // Arrange
        var desc1 = StudyDescription.Empty;
        var desc2 = StudyDescription.Create(null).Value;

        // Assert
        desc1.Should().Be(desc2);
    }

    #endregion

    #region ToString and Implicit Conversion

    [Fact]
    public void ToString_WithValue_ShouldReturnDescriptionText()
    {
        // Arrange
        var description = StudyDescription.Create("Test description").Value;

        // Act
        var result = description.ToString();

        // Assert
        result.Should().Be("Test description");
    }

    [Fact]
    public void ToString_WithEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var description = StudyDescription.Empty;

        // Act
        var result = description.ToString();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
