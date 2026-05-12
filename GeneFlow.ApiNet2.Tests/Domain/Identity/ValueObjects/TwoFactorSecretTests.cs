using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the TwoFactorSecret value object.
/// </summary>
public class TwoFactorSecretTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidSecret_ShouldReturnSuccess()
    {
        // Arrange
        const string encryptedSecret = "encrypted_totp_secret_base32";

        // Act
        var result = TwoFactorSecret.Create(encryptedSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EncryptedSecret.Should().Be(encryptedSecret);
    }

    [Fact]
    public void Create_WithValidSecret_ShouldSetCreatedAtToUtcNow()
    {
        // Arrange
        const string encryptedSecret = "encrypted_totp_secret";
        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = TwoFactorSecret.Create(encryptedSecret);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithCustomCreatedAt_ShouldUseProvidedValue()
    {
        // Arrange
        const string encryptedSecret = "encrypted_totp_secret";
        var customCreatedAt = DateTime.UtcNow.AddHours(-2);

        // Act
        var result = TwoFactorSecret.Create(encryptedSecret, customCreatedAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(customCreatedAt);
    }

    #endregion

    #region Create - Invalid Cases

    [Fact]
    public void Create_WithNullSecret_ShouldReturnFailure()
    {
        // Arrange
        string? encryptedSecret = null;

        // Act
        var result = TwoFactorSecret.Create(encryptedSecret);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.TwoFactorSecretRequired);
    }

    [Fact]
    public void Create_WithEmptySecret_ShouldReturnFailure()
    {
        // Arrange
        const string encryptedSecret = "";

        // Act
        var result = TwoFactorSecret.Create(encryptedSecret);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.TwoFactorSecretRequired);
    }

    [Fact]
    public void Create_WithWhitespaceSecret_ShouldReturnFailure()
    {
        // Arrange
        const string encryptedSecret = "   ";

        // Act
        var result = TwoFactorSecret.Create(encryptedSecret);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.TwoFactorSecretRequired);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnMaskedValue()
    {
        // Arrange
        var secret = TwoFactorSecret.Create("sensitive_secret_value").Value;

        // Act
        var stringValue = secret.ToString();

        // Assert
        stringValue.Should().Be("****");
        stringValue.Should().NotContain("sensitive_secret_value");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameEncryptedSecret_ShouldReturnTrue()
    {
        // Arrange
        const string encryptedSecret = "same_encrypted_secret";
        var secret1 = TwoFactorSecret.Create(encryptedSecret).Value;
        var secret2 = TwoFactorSecret.Create(encryptedSecret).Value;

        // Act & Assert
        secret1.Should().Be(secret2);
    }

    [Fact]
    public void Equals_WithDifferentEncryptedSecret_ShouldReturnFalse()
    {
        // Arrange
        var secret1 = TwoFactorSecret.Create("secret_one").Value;
        var secret2 = TwoFactorSecret.Create("secret_two").Value;

        // Act & Assert
        secret1.Should().NotBe(secret2);
    }

    #endregion
}
