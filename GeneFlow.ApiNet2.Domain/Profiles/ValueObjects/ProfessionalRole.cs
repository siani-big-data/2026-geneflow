using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

/// <summary>
/// Represents a professional role.
/// </summary>
public sealed class ProfessionalRole : ValueObject
{
    /// <summary>Maximum length of professional role.</summary>
    public const int MaxLength = 100;

    /// <summary>Gets the professional role value.</summary>
    public string? Value { get; }

    private ProfessionalRole(string? value) => Value = value;

    /// <summary>
    /// Creates a validated ProfessionalRole.
    /// </summary>
    public static Result<ProfessionalRole> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new ProfessionalRole(null);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<ProfessionalRole>(ProfileErrors.ProfessionalRoleTooLong(MaxLength));

        return new ProfessionalRole(trimmed);
    }

    /// <summary>
    /// Creates an empty ProfessionalRole.
    /// </summary>
    public static ProfessionalRole Empty => new(null);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Implicitly converts to string.</summary>
    public static implicit operator string?(ProfessionalRole role) => role.Value;
}
