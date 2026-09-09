using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

/// <summary>
/// Value object representing a plan name.
/// </summary>
public sealed class PlanName : ValueObject
{
    /// <summary>Minimum length for plan name.</summary>
    public const int MinLength = 2;

    /// <summary>Maximum length for plan name.</summary>
    public const int MaxLength = 50;

    /// <summary>Gets the plan name value.</summary>
    public string Value { get; }

    private PlanName(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a validated PlanName.
    /// </summary>
    public static Result<PlanName> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            return Result.Failure<PlanName>(PlanErrors.NameRequired);

        if (trimmed.Length < MinLength)
            return Result.Failure<PlanName>(PlanErrors.NameTooShort(MinLength));

        if (trimmed.Length > MaxLength)
            return Result.Failure<PlanName>(PlanErrors.NameTooLong(MaxLength));

        return new PlanName(trimmed);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
