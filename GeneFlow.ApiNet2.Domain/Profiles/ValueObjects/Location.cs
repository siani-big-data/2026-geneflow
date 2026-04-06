using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

/// <summary>
/// Represents a profile location.
/// </summary>
public sealed class Location : ValueObject
{
    /// <summary>Maximum length of location.</summary>
    public const int MaxLength = 200;

    /// <summary>Gets the location value.</summary>
    public string? Value { get; }

    private Location(string? value) => Value = value;

    /// <summary>
    /// Creates a validated Location.
    /// </summary>
    public static Result<Location> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new Location(null);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<Location>(ProfileErrors.LocationTooLong(MaxLength));

        return new Location(trimmed);
    }

    /// <summary>
    /// Creates an empty Location.
    /// </summary>
    public static Location Empty => new(null);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Implicitly converts to string.</summary>
    public static implicit operator string?(Location location) => location.Value;
}
