using GeneFlow.ApiNet2.Domain.Identity.Validators;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Validators;

/// <summary>
/// Unit tests for the TwoFactorSecretValidator class.
/// </summary>
public class TwoFactorSecretValidatorTests
{
    private readonly TwoFactorSecretValidator _validator = new();

    #region Valid Secret Tests

    [Fact]
    public void Validate_WithValidSecret_ShouldReturnSuccess()
    {
        // Arrange
        const string secret = "JBSWY3DPEHPK3PXP";

        // Act
        var result = _validator.Validate(secret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secret);
    }

    [Theory]
    [InlineData("JBSWY3DPEHPK3PXP")]
    [InlineData("encrypted_totp_secret_base64")]
    [InlineData("AES256_ENCRYPTED_SECRET_HERE")]
    [InlineData("a")]
    public void Validate_WithVariousValidSecrets_ShouldReturnSuccess(string secret)
    {
        // Act
        var result = _validator.Validate(secret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secret);
    }

    [Fact]
    public void Validate_WithLongSecret_ShouldReturnSuccess()
    {
        // Arrange - A long encrypted secret string
        var secret = new string('X', 256);

        // Act
        var result = _validator.Validate(secret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secret);
    }

    [Fact]
    public void Validate_WithBase32Secret_ShouldReturnSuccess()
    {
        // Arrange - Typical TOTP secret in Base32 format
        const string secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        // Act
        var result = _validator.Validate(secret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secret);
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
        result.Error.Code.Should().Be("User.TwoFactorSecretRequired");
    }

    [Fact]
    public void Validate_WithEmptyString_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.TwoFactorSecretRequired");
    }

    [Fact]
    public void Validate_WithWhitespace_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate("   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.TwoFactorSecretRequired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("  \t  \n  ")]
    public void Validate_WithNullOrWhitespace_ShouldReturnFailure(string? secret)
    {
        // Act
        var result = _validator.Validate(secret);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.TwoFactorSecretRequired");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithSingleCharacter_ShouldReturnSuccess()
    {
        // Arrange
        const string secret = "X";

        // Act
        var result = _validator.Validate(secret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secret);
    }

    [Fact]
    public void Validate_WithLeadingAndTrailingSpaces_ShouldReturnSuccess()
    {
        // Arrange - Has content, so should pass
        const string secret = "  JBSWY3DPEHPK3PXP  ";

        // Act
        var result = _validator.Validate(secret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secret);
    }

    [Fact]
    public void Validate_WithSpecialCharacters_ShouldReturnSuccess()
    {
        // Arrange - Encrypted secret may contain special characters
        const string secret = "enc:AES256/CBC/PKCS7+base64==";

        // Act
        var result = _validator.Validate(secret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secret);
    }

    #endregion
}
