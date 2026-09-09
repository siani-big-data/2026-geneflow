namespace GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;

/// <summary>
/// Two-factor authentication configuration settings.
/// </summary>
public sealed class TwoFactorSettings
{
    public const string SectionName = "TwoFactor";

    /// <summary>
    /// The issuer name displayed in authenticator apps.
    /// </summary>
    public string Issuer { get; set; } = "GeneFlow";

    /// <summary>
    /// Encryption key for storing TOTP secrets.
    /// Should be at least 32 characters for security.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;
}
