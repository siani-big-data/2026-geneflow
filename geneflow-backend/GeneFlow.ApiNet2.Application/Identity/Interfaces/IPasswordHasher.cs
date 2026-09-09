namespace GeneFlow.ApiNet2.Application.Identity.Interfaces;

/// <summary>
/// Service for hashing and verifying passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <returns>The hashed password.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <param name="hash">The password hash to verify against.</param>
    /// <returns>True if the password matches; otherwise, false.</returns>
    bool Verify(string password, string hash);
}
