using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

/// <summary>
/// Represents a profile biography.
/// </summary>
public sealed class Bio : ValueObject
{
    /// <summary>Minimum length of bio (if provided).</summary>
    public const int MinLength = 10;

    /// <summary>Maximum length of bio.</summary>
    public const int MaxLength = 500;

    /// <summary>Gets the bio value.</summary>
    public string? Value { get; }

    private Bio(string? value) => Value = value;

    /// <summary>
    /// Creates a validated Bio.
    /// </summary>
    public static Result<Bio> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new Bio(null);

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
            return Result.Failure<Bio>(ProfileErrors.BioTooShort(MinLength));

        if (trimmed.Length > MaxLength)
            return Result.Failure<Bio>(ProfileErrors.BioTooLong(MaxLength));

        return new Bio(trimmed);
    }

    /// <summary>
    /// Creates an empty Bio.
    /// </summary>
    public static Bio Empty => new(null);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Implicitly converts to string.</summary>
    public static implicit operator string?(Bio bio) => bio.Value;
}
