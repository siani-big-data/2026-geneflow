using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Validators;

/// <summary>
/// Unit tests for the PasswordValidator class.
/// </summary>
public class PasswordValidatorTests
{
    private readonly PasswordValidator _validator = new();

    #region Valid Password Tests

    [Fact]
    public void Validate_WithValidPassword_ShouldReturnSuccess()
    {
        // Arrange
        const string password = "ValidPass1";

        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(password);
    }

    [Theory]
    [InlineData("Password1")]
    [InlineData("MySecure123")]
    [InlineData("TestPass99")]
    [InlineData("Abcdefgh1")]
    public void Validate_WithVariousValidPasswords_ShouldReturnSuccess(string password)
    {
        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(password);
    }

    [Fact]
    public void Validate_WithMinLengthPassword_ShouldReturnSuccess()
    {
        // Arrange - exactly MinLength characters with required complexity
        var password = "Abcdef1a"; // 8 characters

        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMaxLengthPassword_ShouldReturnSuccess()
    {
        // Arrange - exactly MaxLength characters with required complexity
        var password = "A" + new string('a', Password.MaxLength - 2) + "1";

        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Null and Empty Tests

    [Fact]
    public void Validate_WithNullPassword_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordTooShort");
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordTooShort");
    }

    #endregion

    #region Length Validation Tests

    [Fact]
    public void Validate_WithTooShortPassword_ShouldReturnFailure()
    {
        // Arrange - less than MinLength
        var password = new string('A', Password.MinLength - 1);

        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordTooShort");
    }

    [Fact]
    public void Validate_WithTooLongPassword_ShouldReturnFailure()
    {
        // Arrange - more than MaxLength
        var password = "A" + new string('a', Password.MaxLength) + "1";

        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordTooLong");
    }

    #endregion

    #region Complexity Requirements Tests

    [Fact]
    public void Validate_WithoutUppercase_ShouldReturnFailure()
    {
        // Arrange - no uppercase letter
        const string password = "lowercase1";

        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordRequiresUppercase");
    }

    [Fact]
    public void Validate_WithoutLowercase_ShouldReturnFailure()
    {
        // Arrange - no lowercase letter
        const string password = "UPPERCASE1";

        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordRequiresLowercase");
    }

    [Fact]
    public void Validate_WithoutDigit_ShouldReturnFailure()
    {
        // Arrange - no digit
        const string password = "NoDigitHere";

        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordRequiresDigit");
    }

    [Theory]
    [InlineData("12345678")]  // Only digits
    [InlineData("abcdefgh")]  // Only lowercase
    [InlineData("ABCDEFGH")]  // Only uppercase
    public void Validate_WithSingleCharacterType_ShouldReturnFailure(string password)
    {
        // Act
        var result = _validator.Validate(password);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion
}
