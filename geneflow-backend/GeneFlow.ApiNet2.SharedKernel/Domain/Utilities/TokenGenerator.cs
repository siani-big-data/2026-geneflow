namespace GeneFlow.ApiNet2.SharedKernel.Domain.Utilities;

/// <summary>
/// Utility class for generating URL-safe tokens.
/// </summary>
public static class TokenGenerator
{
    /// <summary>
    /// Generates a URL-safe token based on a new GUID.
    /// The token is Base64 encoded with URL-safe character replacements.
    /// </summary>
    /// <returns>A 22-character URL-safe token string.</returns>
    public static string GenerateUrlSafeToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=');
    }

    /// <summary>
    /// Generates a URL-safe token with a specified length.
    /// Uses cryptographically secure random bytes.
    /// </summary>
    /// <param name="byteLength">Number of random bytes to generate (default: 32).</param>
    /// <returns>A URL-safe token string.</returns>
    public static string GenerateSecureToken(int byteLength = 32)
    {
        var bytes = new byte[byteLength];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=');
    }
}
