using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.ValueObjects;

/// <summary>
/// Unit tests for the Bio value object.
/// </summary>
public class BioTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidBio_ShouldReturnSuccess()
    {
        // Arrange
        const string bioText = "Researcher specializing in genomics and bioinformatics.";

        // Act
        var result = Bio.Create(bioText);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(bioText);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldReturnSuccessWithNullValue(string? value)
    {
        // Act
        var result = Bio.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        const string bioText = "  Some bio text  ";

        // Act
        var result = Bio.Create(bioText);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Some bio text");
    }

    [Fact]
    public void Create_WithMaxLength_ShouldReturnSuccess()
    {
        // Arrange
        var maxLengthBio = new string('a', Bio.MaxLength);

        // Act
        var result = Bio.Create(maxLengthBio);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().HaveLength(Bio.MaxLength);
    }

    #endregion

    #region Create - Invalid Cases

    [Fact]
    public void Create_WithBioTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longBio = new string('a', Bio.MaxLength + 1);

        // Act
        var result = Bio.Create(longBio);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Bio");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldReturnBioWithNullValue()
    {
        // Act
        var bio = Bio.Empty;

        // Assert
        bio.Value.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var bio1 = Bio.Create("Test bio text here").Value;
        var bio2 = Bio.Create("Test bio text here").Value;

        // Assert
        bio1.Should().Be(bio2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var bio1 = Bio.Create("Bio one text here").Value;
        var bio2 = Bio.Create("Bio two text here").Value;

        // Assert
        bio1.Should().NotBe(bio2);
    }

    [Fact]
    public void Equals_BothEmpty_ShouldReturnTrue()
    {
        // Arrange
        var bio1 = Bio.Empty;
        var bio2 = Bio.Create(null).Value;

        // Assert
        bio1.Should().Be(bio2);
    }

    #endregion

    #region ToString and Implicit Conversion

    [Fact]
    public void ToString_WithValue_ShouldReturnBioText()
    {
        // Arrange
        var bio = Bio.Create("Test bio text here").Value;

        // Act
        var result = bio.ToString();

        // Assert
        result.Should().Be("Test bio text here");
    }

    [Fact]
    public void ToString_WithEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var bio = Bio.Empty;

        // Act
        var result = bio.ToString();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnBioValue()
    {
        // Arrange
        var bio = Bio.Create("Test bio text here").Value;

        // Act
        string? result = bio;

        // Assert
        result.Should().Be("Test bio text here");
    }

    #endregion
}
