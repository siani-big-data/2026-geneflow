using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Represents a hashed password.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    private const string PlaceholderValue = "OAUTH_NO_PASSWORD";
    private static readonly PasswordHashValidator Validator = new();

    /// <summary>Gets the hashed password value.</summary>
    public string Value { get; }

    /// <summary>Gets whether this is a placeholder for OAuth users without password.</summary>
    public bool IsPlaceholder => Value == PlaceholderValue;

    private PasswordHash(string value) => Value = value;

    /// <summary>
    /// Creates a validated password hash.
    /// </summary>
    /// <param name="hashedValue">The hashed password string.</param>
    /// <returns>A result containing the hash or validation error.</returns>
    public static Result<PasswordHash> Create(string? hashedValue)
    {
        return Validator.Validate(hashedValue).Map(v => new PasswordHash(v));
    }

    /// <summary>
    /// Creates a placeholder hash for OAuth users.
    /// </summary>
    /// <returns>A placeholder password hash.</returns>
    public static PasswordHash CreatePlaceholder() => new(PlaceholderValue);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => "****";

    /// <summary>Implicitly converts to string.</summary>
    public static implicit operator string(PasswordHash hash) => hash.Value;
}
