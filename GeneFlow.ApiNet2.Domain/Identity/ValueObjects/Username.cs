using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

/// <summary>
/// Represents a validated username.
/// </summary>
public sealed class Username : ValueObject
{
    /// <summary>Minimum length of a username.</summary>
    public const int MinLength = 3;

    /// <summary>Maximum length of a username.</summary>
    public const int MaxLength = 50;

    private static readonly UsernameValidator Validator = new();

    /// <summary>Gets the username value.</summary>
    public string Value { get; }

    private Username(string value) => Value = value;

    /// <summary>
    /// Creates a validated username.
    /// </summary>
    /// <param name="value">The username string.</param>
    /// <returns>A result containing the username or validation error.</returns>
    public static Result<Username> Create(string? value)
    {
        return Validator.Validate(value).Map(v => new Username(v));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Implicitly converts to string.</summary>
    public static implicit operator string(Username username) => username.Value;
}
