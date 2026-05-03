using System.Security.Cryptography;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Identity.Entities;

/// <summary>
/// Represents a two-factor authentication code sent via email.
/// </summary>
public sealed class TwoFactorCode : Entity<Guid>
{
    private static readonly TimeSpan CodeValidityPeriod = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets the 6-digit verification code.
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Gets the timestamp when the code was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the code expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets whether the code has been used.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// Gets the timestamp when the code was used, if applicable.
    /// </summary>
    public DateTime? UsedAt { get; private set; }

    private TwoFactorCode() : base(Guid.NewGuid()) { }

    private TwoFactorCode(string code) : base(Guid.NewGuid())
    {
        Code = code;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(CodeValidityPeriod);
        IsUsed = false;
    }

    /// <summary>
    /// Creates a new two-factor authentication code.
    /// </summary>
    /// <returns>A new TwoFactorCode instance with a generated 6-digit code.</returns>
    public static TwoFactorCode Create()
    {
        var code = GenerateCode();
        return new TwoFactorCode(code);
    }

    /// <summary>
    /// Gets whether the code is still valid (not used and not expired).
    /// </summary>
    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Gets whether the code has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Marks the code as used.
    /// </summary>
    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the provided code against this instance.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>True if the code matches and is still valid; otherwise, false.</returns>
    public bool Validate(string code)
    {
        return Code == code && IsValid;
    }

    private static string GenerateCode()
    {
        // Use cryptographically secure random number generator
        var code = RandomNumberGenerator.GetInt32(100000, 1000000);
        return code.ToString("D6");
    }
}
