using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity;

/// <summary>
/// Unit tests for the UserId strongly-typed identifier.
/// </summary>
public class UserIdTests
{
    #region Constructor

    [Fact]
    public void Constructor_ShouldCreateValidUserId()
    {
        // Act
        var userId = new UserId(1);

        // Assert
        userId.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithZero_ShouldCreateUserId()
    {
        // Act
        var userId = new UserId(0);

        // Assert
        userId.Should().NotBeNull();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnFormattedId()
    {
        // Arrange
        var userId = new UserId(1);

        // Act
        var result = userId.ToString();

        // Assert
        result.Should().Be("U00000001");
    }

    [Fact]
    public void ToString_WithLargerNumber_ShouldPadCorrectly()
    {
        // Arrange
        var userId = new UserId(12345);

        // Act
        var result = userId.ToString();

        // Assert
        result.Should().Be("U00012345");
    }

    [Fact]
    public void ToString_WithMaxDigits_ShouldNotPad()
    {
        // Arrange
        var userId = new UserId(12345678);

        // Act
        var result = userId.ToString();

        // Assert
        result.Should().Be("U12345678");
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_WithValidId_ShouldReturnUserId()
    {
        // Arrange
        const string id = "U00000001";

        // Act
        var userId = UserId.Parse(id);

        // Assert
        userId.Should().NotBeNull();
        userId.ToString().Should().Be(id);
    }

    [Fact]
    public void Parse_WithLargerNumber_ShouldReturnCorrectUserId()
    {
        // Arrange
        const string id = "U00012345";

        // Act
        var userId = UserId.Parse(id);

        // Assert
        userId.ToString().Should().Be(id);
    }

    [Fact]
    public void Parse_WithInvalidPrefix_ShouldThrow()
    {
        // Arrange
        const string id = "X00000001";

        // Act & Assert
        var act = () => UserId.Parse(id);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Parse_WithInvalidFormat_ShouldThrow()
    {
        // Arrange
        const string id = "U123";

        // Act & Assert
        var act = () => UserId.Parse(id);
        act.Should().Throw<Exception>();
    }

    #endregion

    #region TryParse

    [Fact]
    public void TryParse_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        const string id = "U00000001";

        // Act
        var success = UserId.TryParse(id, out var userId);

        // Assert
        success.Should().BeTrue();
        userId.Should().NotBeNull();
        userId!.ToString().Should().Be(id);
    }

    [Fact]
    public void TryParse_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        const string id = "invalid";

        // Act
        var success = UserId.TryParse(id, out var userId);

        // Assert
        success.Should().BeFalse();
        userId.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithNull_ShouldReturnFalse()
    {
        // Act
        var success = UserId.TryParse(null, out var userId);

        // Assert
        success.Should().BeFalse();
        userId.Should().BeNull();
    }

    #endregion

    #region FromSequence

    [Fact]
    public void FromSequence_ShouldCreateUserId()
    {
        // Arrange
        const long sequenceValue = 42;

        // Act
        var userId = UserId.FromSequence(sequenceValue);

        // Assert
        userId.ToString().Should().Be("U00000042");
    }

    #endregion

    #region SequenceName

    [Fact]
    public void SequenceName_ShouldBeUsers()
    {
        UserId.SequenceName.Should().Be("users");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var userId1 = new UserId(1);
        var userId2 = new UserId(1);

        // Assert
        userId1.Should().Be(userId2);
        (userId1 == userId2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var userId1 = new UserId(1);
        var userId2 = new UserId(2);

        // Assert
        userId1.Should().NotBe(userId2);
        (userId1 != userId2).Should().BeTrue();
    }

    #endregion
}
