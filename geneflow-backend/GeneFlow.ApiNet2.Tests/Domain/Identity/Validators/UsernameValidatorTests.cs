using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Validators;

/// <summary>
/// Unit tests for the UsernameValidator class.
/// </summary>
public class UsernameValidatorTests
{
    private readonly UsernameValidator _validator = new();

    #region Valid Username Tests

    [Theory]
    [InlineData("usr")]
    [InlineData("user123")]
    [InlineData("johndoe")]
    [InlineData("username123")]
    [InlineData("ABC123")]
    public void Validate_WithValidUsername_ShouldReturnSuccess(string username)
    {
        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(username.ToLowerInvariant());
    }

    [Fact]
    public void Validate_WithMixedCaseUsername_ShouldNormalizeToLowercase()
    {
        // Arrange
        const string username = "JohnDoe";

        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("johndoe");
    }

    [Fact]
    public void Validate_WithMinLengthUsername_ShouldReturnSuccess()
    {
        // Arrange
        var username = new string('a', Username.MinLength);

        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMaxLengthUsername_ShouldReturnSuccess()
    {
        // Arrange
        var username = new string('a', Username.MaxLength);

        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithLeadingAndTrailingWhitespace_ShouldTrim()
    {
        // Arrange
        const string username = "  testuser  ";

        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("testuser");
    }

    #endregion

    #region Null and Empty Tests

    [Fact]
    public void Validate_WithNullUsername_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.UsernameRequired");
    }

    [Fact]
    public void Validate_WithEmptyUsername_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.UsernameRequired");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyUsername_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate("   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.UsernameRequired");
    }

    #endregion

    #region Length Validation Tests

    [Fact]
    public void Validate_WithTooShortUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = new string('a', Username.MinLength - 1);

        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.UsernameTooShort");
    }

    [Fact]
    public void Validate_WithTooLongUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = new string('a', Username.MaxLength + 1);

        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.UsernameTooLong");
    }

    #endregion

    #region Invalid Character Tests

    [Theory]
    [InlineData("user@name")]
    [InlineData("user name")]
    [InlineData("user.name")]
    [InlineData("user-name")]
    [InlineData("user!name")]
    [InlineData("user_name")]
    [InlineData("user#name")]
    [InlineData("user$name")]
    public void Validate_WithInvalidCharacters_ShouldReturnFailure(string username)
    {
        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.UsernameInvalidFormat");
    }

    [Theory]
    [InlineData("abc123")]  // Lowercase and digits
    [InlineData("ABC123")]  // Uppercase and digits
    [InlineData("abcDEF")]  // Mixed case
    [InlineData("123456")]  // Only digits
    public void Validate_WithOnlyAlphanumericCharacters_ShouldReturnSuccess(string username)
    {
        // Act
        var result = _validator.Validate(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
