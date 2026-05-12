using GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;
using GeneFlow.ApiNet2.Infrastructure.Identity.Services;
using Microsoft.Extensions.Options;
using OtpNet;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for the TwoFactorAuthenticator service.
/// Tests TOTP generation, validation, and secret encryption/decryption.
/// </summary>
public class TwoFactorAuthenticatorTests
{
    private readonly TwoFactorSettings _settings;
    private readonly TwoFactorAuthenticator _authenticator;

    public TwoFactorAuthenticatorTests()
    {
        _settings = new TwoFactorSettings
        {
            Issuer = "GeneFlow",
            EncryptionKey = "ThisIsAVerySecureEncryptionKeyForTesting123!"
        };

        var options = Options.Create(_settings);
        _authenticator = new TwoFactorAuthenticator(options);
    }

    #region GenerateSecret

    [Fact]
    public void GenerateSecret_ShouldReturnBase32EncodedString()
    {
        // Act
        var secret = _authenticator.GenerateSecret();

        // Assert
        secret.Should().NotBeNullOrEmpty();
        // Base32 characters are A-Z and 2-7
        secret.Should().MatchRegex("^[A-Z2-7]+$");
    }

    [Fact]
    public void GenerateSecret_ShouldReturnDifferentSecrets()
    {
        // Act
        var secret1 = _authenticator.GenerateSecret();
        var secret2 = _authenticator.GenerateSecret();

        // Assert
        secret1.Should().NotBe(secret2);
    }

    [Fact]
    public void GenerateSecret_ShouldReturnValidBase32Length()
    {
        // Act
        var secret = _authenticator.GenerateSecret();

        // Assert - 20 bytes encoded in Base32 should be 32 characters
        secret.Length.Should().Be(32);
    }

    #endregion

    #region GenerateQrCodeUri

    [Fact]
    public void GenerateQrCodeUri_ShouldReturnValidOtpAuthUri()
    {
        // Arrange
        var email = "test@example.com";
        var secret = _authenticator.GenerateSecret();

        // Act
        var uri = _authenticator.GenerateQrCodeUri(email, secret);

        // Assert
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain(secret);
        uri.Should().Contain("test%40example.com");
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldUseSettingsIssuer()
    {
        // Arrange
        var email = "user@domain.com";
        var secret = _authenticator.GenerateSecret();

        // Act
        var uri = _authenticator.GenerateQrCodeUri(email, secret);

        // Assert
        uri.Should().Contain("GeneFlow");
        uri.Should().Contain("issuer=GeneFlow");
    }

    [Fact]
    public void GenerateQrCodeUri_ShouldEncodeSpecialCharacters()
    {
        // Arrange
        var email = "user+test@example.com";
        var secret = _authenticator.GenerateSecret();

        // Act
        var uri = _authenticator.GenerateQrCodeUri(email, secret);

        // Assert
        uri.Should().NotContain("+"); // Should be encoded as %2B
        uri.Should().Contain("%2B");
    }

    #endregion

    #region ValidateCode

    [Fact]
    public void ValidateCode_WithValidCode_ShouldReturnTrue()
    {
        // Arrange
        var secret = _authenticator.GenerateSecret();
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);
        var validCode = totp.ComputeTotp();

        // Act
        var result = _authenticator.ValidateCode(secret, validCode);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_WithInvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var secret = _authenticator.GenerateSecret();
        var invalidCode = "000000";

        // Act
        var result = _authenticator.ValidateCode(secret, invalidCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_WithMalformedSecret_ShouldReturnFalse()
    {
        // Arrange
        var malformedSecret = "INVALID!!!SECRET";
        var code = "123456";

        // Act
        var result = _authenticator.ValidateCode(malformedSecret, code);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region EncryptSecret / DecryptSecret

    [Fact]
    public void EncryptSecret_ShouldReturnBase64EncodedString()
    {
        // Arrange
        var secret = _authenticator.GenerateSecret();

        // Act
        var encrypted = _authenticator.EncryptSecret(secret);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(secret);
        // Should be valid Base64
        var action = () => Convert.FromBase64String(encrypted);
        action.Should().NotThrow();
    }

    [Fact]
    public void DecryptSecret_ShouldRecoverOriginalSecret()
    {
        // Arrange
        var originalSecret = _authenticator.GenerateSecret();
        var encrypted = _authenticator.EncryptSecret(originalSecret);

        // Act
        var decrypted = _authenticator.DecryptSecret(encrypted);

        // Assert
        decrypted.Should().Be(originalSecret);
    }

    [Fact]
    public void EncryptSecret_ShouldProduceDifferentCiphertext()
    {
        // Arrange - Same secret encrypted twice should produce different ciphertexts (due to random IV)
        var secret = _authenticator.GenerateSecret();

        // Act
        var encrypted1 = _authenticator.EncryptSecret(secret);
        var encrypted2 = _authenticator.EncryptSecret(secret);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ShouldPreserveAllSecrets()
    {
        // Arrange & Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var secret = _authenticator.GenerateSecret();
            var encrypted = _authenticator.EncryptSecret(secret);
            var decrypted = _authenticator.DecryptSecret(encrypted);

            decrypted.Should().Be(secret);
        }
    }

    #endregion
}
