using GeneFlow.ApiNet2.Domain.Identity.Validators;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Validators;

/// <summary>
/// Unit tests for the PasswordHashValidator class.
/// </summary>
public class PasswordHashValidatorTests
{
    private readonly PasswordHashValidator _validator = new();

    #region Valid Hash Tests

    [Fact]
    public void Validate_WithValidHash_ShouldReturnSuccess()
    {
        // Arrange
        const string hash = "$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZRGdjGj/n3.";

        // Act
        var result = _validator.Validate(hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(hash);
    }

    [Theory]
    [InlineData("simple_hash_value")]
    [InlineData("$argon2id$v=19$m=65536,t=3,p=4$...")]
    [InlineData("$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/X4.")]
    [InlineData("pbkdf2_sha256$260000$...")]
    public void Validate_WithVariousValidHashes_ShouldReturnSuccess(string hash)
    {
        // Act
        var result = _validator.Validate(hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(hash);
    }

    [Fact]
    public void Validate_WithLongHash_ShouldReturnSuccess()
    {
        // Arrange - A very long hash string
        var hash = new string('a', 500);

        // Act
        var result = _validator.Validate(hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(hash);
    }

    #endregion

    #region Null and Empty Tests

    [Fact]
    public void Validate_WithNull_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordHashRequired");
    }

    [Fact]
    public void Validate_WithEmptyString_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordHashRequired");
    }

    [Fact]
    public void Validate_WithWhitespace_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate("   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordHashRequired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("  \t  \n  ")]
    public void Validate_WithNullOrWhitespace_ShouldReturnFailure(string? hash)
    {
        // Act
        var result = _validator.Validate(hash);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordHashRequired");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithSingleCharacter_ShouldReturnSuccess()
    {
        // Arrange
        const string hash = "x";

        // Act
        var result = _validator.Validate(hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(hash);
    }

    [Fact]
    public void Validate_WithLeadingAndTrailingSpaces_ShouldReturnSuccess()
    {
        // Arrange - Has content, so should pass
        const string hash = "  hash_with_spaces  ";

        // Act
        var result = _validator.Validate(hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(hash);
    }

    #endregion
}
