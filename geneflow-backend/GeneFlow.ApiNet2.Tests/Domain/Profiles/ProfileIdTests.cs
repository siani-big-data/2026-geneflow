using GeneFlow.ApiNet2.Domain.Profiles;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles;

/// <summary>
/// Unit tests for the ProfileId strongly-typed identifier.
/// </summary>
public class ProfileIdTests
{
    #region Constructor

    [Fact]
    public void Constructor_ShouldCreateValidProfileId()
    {
        // Act
        var profileId = new ProfileId(1);

        // Assert
        profileId.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithZero_ShouldCreateProfileId()
    {
        // Act
        var profileId = new ProfileId(0);

        // Assert
        profileId.Should().NotBeNull();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnFormattedId()
    {
        // Arrange
        var profileId = new ProfileId(1);

        // Act
        var result = profileId.ToString();

        // Assert
        result.Should().Be("P00000001");
    }

    [Fact]
    public void ToString_WithLargerNumber_ShouldPadCorrectly()
    {
        // Arrange
        var profileId = new ProfileId(12345);

        // Act
        var result = profileId.ToString();

        // Assert
        result.Should().Be("P00012345");
    }

    [Fact]
    public void ToString_WithMaxDigits_ShouldNotPad()
    {
        // Arrange
        var profileId = new ProfileId(12345678);

        // Act
        var result = profileId.ToString();

        // Assert
        result.Should().Be("P12345678");
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_WithValidId_ShouldReturnProfileId()
    {
        // Arrange
        const string id = "P00000001";

        // Act
        var profileId = ProfileId.Parse(id);

        // Assert
        profileId.Should().NotBeNull();
        profileId.ToString().Should().Be(id);
    }

    [Fact]
    public void Parse_WithLargerNumber_ShouldReturnCorrectProfileId()
    {
        // Arrange
        const string id = "P00012345";

        // Act
        var profileId = ProfileId.Parse(id);

        // Assert
        profileId.ToString().Should().Be(id);
    }

    [Fact]
    public void Parse_WithInvalidPrefix_ShouldThrow()
    {
        // Arrange
        const string id = "U00000001";

        // Act & Assert
        var act = () => ProfileId.Parse(id);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_WithInvalidFormat_ShouldThrow()
    {
        // Arrange
        const string id = "P123";

        // Act & Assert
        var act = () => ProfileId.Parse(id);
        act.Should().Throw<Exception>();
    }

    #endregion

    #region TryParse

    [Fact]
    public void TryParse_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        const string id = "P00000001";

        // Act
        var success = ProfileId.TryParse(id, out var profileId);

        // Assert
        success.Should().BeTrue();
        profileId.Should().NotBeNull();
        profileId!.ToString().Should().Be(id);
    }

    [Fact]
    public void TryParse_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        const string id = "invalid";

        // Act
        var success = ProfileId.TryParse(id, out var profileId);

        // Assert
        success.Should().BeFalse();
        profileId.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithNull_ShouldReturnFalse()
    {
        // Act
        var success = ProfileId.TryParse(null, out var profileId);

        // Assert
        success.Should().BeFalse();
        profileId.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithWrongPrefix_ShouldReturnFalse()
    {
        // Arrange
        const string id = "U00000001";

        // Act
        var success = ProfileId.TryParse(id, out var profileId);

        // Assert
        success.Should().BeFalse();
        profileId.Should().BeNull();
    }

    #endregion

    #region FromSequence

    [Fact]
    public void FromSequence_ShouldCreateProfileId()
    {
        // Arrange
        const long sequenceValue = 42;

        // Act
        var profileId = ProfileId.FromSequence(sequenceValue);

        // Assert
        profileId.ToString().Should().Be("P00000042");
    }

    #endregion

    #region SequenceName

    [Fact]
    public void SequenceName_ShouldBeProfiles()
    {
        ProfileId.SequenceName.Should().Be("profiles");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var profileId1 = new ProfileId(1);
        var profileId2 = new ProfileId(1);

        // Assert
        profileId1.Should().Be(profileId2);
        (profileId1 == profileId2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var profileId1 = new ProfileId(1);
        var profileId2 = new ProfileId(2);

        // Assert
        profileId1.Should().NotBe(profileId2);
        (profileId1 != profileId2).Should().BeTrue();
    }

    #endregion
}
