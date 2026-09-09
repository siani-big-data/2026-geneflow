using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Represents an encrypted TOTP secret for two-factor authentication.
/// </summary>
public sealed class TwoFactorSecret : ValueObject
{
    private static readonly TwoFactorSecretValidator Validator = new();

    /// <summary>Gets the encrypted TOTP secret.</summary>
    public string EncryptedSecret { get; }

    /// <summary>Gets when the secret was created.</summary>
    public DateTime CreatedAt { get; }

    private TwoFactorSecret(string encryptedSecret, DateTime createdAt)
    {
        EncryptedSecret = encryptedSecret;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a new TwoFactorSecret.
    /// </summary>
    /// <param name="encryptedSecret">The encrypted TOTP secret.</param>
    /// <param name="createdAt">Optional creation timestamp (defaults to UTC now).</param>
    /// <returns>A result containing the secret or validation error.</returns>
    public static Result<TwoFactorSecret> Create(string? encryptedSecret, DateTime? createdAt = null)
    {
        var created = createdAt ?? DateTime.UtcNow;
        return Validator.Validate(encryptedSecret).Map(v => new TwoFactorSecret(v, created));
    }

    /// <summary>
    /// Reconstitutes a TwoFactorSecret from persistence.
    /// </summary>
    internal static TwoFactorSecret Reconstitute(string encryptedSecret, DateTime createdAt)
    {
        return new TwoFactorSecret(encryptedSecret, createdAt);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return EncryptedSecret;
    }

    /// <inheritdoc />
    public override string ToString() => "****";
}
