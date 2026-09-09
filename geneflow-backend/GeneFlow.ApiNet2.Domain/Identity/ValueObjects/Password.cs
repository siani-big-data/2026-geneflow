using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Represents a validated plain text password.
/// This value object validates password complexity before hashing.
/// </summary>
public sealed class Password : ValueObject
{
    /// <summary>Minimum password length.</summary>
    public const int MinLength = 8;

    /// <summary>Maximum password length.</summary>
    public const int MaxLength = 100;

    private static readonly PasswordValidator Validator = new();

    /// <summary>Gets the plain text password value.</summary>
    public string Value { get; }

    private Password(string value) => Value = value;

    /// <summary>
    /// Creates a validated password.
    /// </summary>
    /// <param name="value">The plain text password.</param>
    /// <returns>A result containing the password or validation error.</returns>
    public static Result<Password> Create(string? value)
    {
        return Validator.Validate(value).Map(v => new Password(v));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => "****";

    /// <summary>Implicitly converts to string.</summary>
    public static implicit operator string(Password password) => password.Value;
}
