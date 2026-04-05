namespace GeneFlow.ApiNet2.Application.Identity.Interfaces;

/// <summary>
/// Service for TOTP-based two-factor authentication.
/// </summary>
public interface ITwoFactorAuthenticator
{
    /// <summary>
    /// Generates a new secret key for TOTP.
    /// </summary>
    /// <returns>A Base32-encoded secret key.</returns>
    string GenerateSecret();

    /// <summary>
    /// Generates a QR code URI for authenticator apps.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="secret">The Base32-encoded secret.</param>
    /// <param name="issuer">The issuer name (defaults to settings or "GeneFlow").</param>
    /// <returns>An otpauth:// URI for QR code generation.</returns>
    string GenerateQrCodeUri(string email, string secret, string issuer = "GeneFlow");

    /// <summary>
    /// Validates a TOTP code against a secret.
    /// </summary>
    /// <param name="secret">The Base32-encoded secret.</param>
    /// <param name="code">The 6-digit code from the authenticator app.</param>
    /// <returns>True if the code is valid; false otherwise.</returns>
    bool ValidateCode(string secret, string code);

    /// <summary>
    /// Encrypts a secret for secure storage.
    /// </summary>
    /// <param name="secret">The plain Base32 secret.</param>
    /// <returns>The encrypted secret.</returns>
    string EncryptSecret(string secret);

    /// <summary>
    /// Decrypts a stored secret.
    /// </summary>
    /// <param name="encryptedSecret">The encrypted secret.</param>
    /// <returns>The plain Base32 secret.</returns>
    string DecryptSecret(string encryptedSecret);
}
