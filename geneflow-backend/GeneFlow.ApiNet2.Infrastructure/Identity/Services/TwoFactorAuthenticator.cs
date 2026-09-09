using System.Security.Cryptography;
using System.Text;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;
using Microsoft.Extensions.Options;
using OtpNet;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Services;

/// <summary>
/// TOTP-based two-factor authentication implementation.
/// </summary>
internal sealed class TwoFactorAuthenticator : ITwoFactorAuthenticator
{
    private readonly TwoFactorSettings _settings;

    public TwoFactorAuthenticator(IOptions<TwoFactorSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    /// <inheritdoc />
    public string GenerateQrCodeUri(string email, string secret, string issuer = "GeneFlow")
    {
        var actualIssuer = !string.IsNullOrWhiteSpace(_settings.Issuer)
            ? _settings.Issuer
            : issuer;

        // Format: otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}
        var encodedIssuer = Uri.EscapeDataString(actualIssuer);
        var encodedEmail = Uri.EscapeDataString(email);

        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}";
    }

    /// <inheritdoc />
    public bool ValidateCode(string secret, string code)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);

            // Verify with a window of 1 step (30 seconds before and after)
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string EncryptSecret(string secret)
    {
        var key = GetEncryptionKey();
        var iv = RandomNumberGenerator.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(secret);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Combine IV + encrypted data
        var result = new byte[iv.Length + encryptedBytes.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    public string DecryptSecret(string encryptedSecret)
    {
        var key = GetEncryptionKey();
        var fullCipher = Convert.FromBase64String(encryptedSecret);

        // Extract IV (first 16 bytes)
        var iv = new byte[16];
        var cipherText = new byte[fullCipher.Length - 16];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
        Buffer.BlockCopy(fullCipher, 16, cipherText, 0, cipherText.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private byte[] GetEncryptionKey()
    {
        // Derive a 256-bit key from the configured encryption key
        var keyBytes = Encoding.UTF8.GetBytes(_settings.EncryptionKey);
        return SHA256.HashData(keyBytes);
    }
}
