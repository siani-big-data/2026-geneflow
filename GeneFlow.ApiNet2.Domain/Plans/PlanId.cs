using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Plans;

/// <summary>
/// Strongly-typed identifier for Plan aggregate.
/// </summary>
public sealed class PlanId : SingleValueObject<Guid>
{
    /// <summary>
    /// Initializes a new instance of PlanId.
    /// </summary>
    public PlanId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new unique PlanId.
    /// </summary>
    public static PlanId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a PlanId from an existing Guid.
    /// </summary>
    public static PlanId From(Guid value) => new(value);

    /// <summary>
    /// Parses a string representation to PlanId.
    /// </summary>
    public static PlanId Parse(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a string to PlanId.
    /// </summary>
    public static bool TryParse(string? value, out PlanId? planId)
    {
        planId = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Guid.TryParse(value, out var guid))
            return false;

        planId = new PlanId(guid);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
