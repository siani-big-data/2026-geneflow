using GeneFlow.ApiNet2.Application.Identity.Interfaces;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Services;

/// <summary>
/// BCrypt-based password hasher implementation.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <inheritdoc />
    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
