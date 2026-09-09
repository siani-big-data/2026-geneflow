using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.ValueObjects;

/// <summary>
/// Unit tests for the StudyTitle value object.
/// </summary>
public class StudyTitleTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidTitle_ShouldReturnSuccess()
    {
        // Arrange
        const string title = "Genomic Analysis of BRCA1 Mutations";

        // Act
        var result = StudyTitle.Create(title);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(title);
    }

    [Fact]
    public void Create_WithMinLength_ShouldReturnSuccess()
    {
        // Arrange
        var minTitle = new string('a', StudyTitle.MinLength);

        // Act
        var result = StudyTitle.Create(minTitle);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(StudyTitle.MinLength);
    }

    [Fact]
    public void Create_WithMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var maxTitle = new string('a', StudyTitle.MaxLength);

        // Act
        var result = StudyTitle.Create(maxTitle);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(StudyTitle.MaxLength);
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        const string title = "  Research Study  ";

        // Act
        var result = StudyTitle.Create(title);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Research Study");
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
        var result = StudyTitle.Create(value!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Title");
    }

    [Fact]
    public void Create_WithTitleTooShort_ShouldReturnFailure()
    {
        // Arrange
        var shortTitle = new string('a', StudyTitle.MinLength - 1);

        // Act
        var result = StudyTitle.Create(shortTitle);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Title");
    }

    [Fact]
    public void Create_WithTitleTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longTitle = new string('a', StudyTitle.MaxLength + 1);

        // Act
        var result = StudyTitle.Create(longTitle);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Title");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var title1 = StudyTitle.Create("Research Study").Value;
        var title2 = StudyTitle.Create("Research Study").Value;

        // Assert
        title1.Should().Be(title2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var title1 = StudyTitle.Create("Study One").Value;
        var title2 = StudyTitle.Create("Study Two").Value;

        // Assert
        title1.Should().NotBe(title2);
    }

    #endregion

    #region ToString and Implicit Conversion

    [Fact]
    public void ToString_ShouldReturnTitleValue()
    {
        // Arrange
        var title = StudyTitle.Create("Test Study").Value;

        // Act
        var result = title.ToString();

        // Assert
        result.Should().Be("Test Study");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnTitleValue()
    {
        // Arrange
        var title = StudyTitle.Create("Test Study").Value;

        // Act
        string result = title;

        // Assert
        result.Should().Be("Test Study");
    }

    #endregion
}
