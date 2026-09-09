using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Represents a validated email address.
/// </summary>
public sealed class Email : ValueObject
{
    /// <summary>Maximum length of an email address.</summary>
    public const int MaxLength = 256;

    private static readonly EmailValidator Validator = new();

    /// <summary>Gets the email address value.</summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Creates a validated email address.
    /// </summary>
    /// <param name="value">The email address string.</param>
    /// <returns>A result containing the email or validation error.</returns>
    public static Result<Email> Create(string? value)
    {
        return Validator.Validate(value).Map(v => new Email(v));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Implicitly converts to string.</summary>
    public static implicit operator string(Email email) => email.Value;
}
