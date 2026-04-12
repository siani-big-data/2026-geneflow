using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Plans;

/// <summary>
/// Strongly-typed identifier for Plan aggregate.
/// Format: L00000001 (L + 8 digits)
/// </summary>
public sealed class PlanId : PrefixedId<PlanId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "plans";

    /// <inheritdoc />
    protected override char Prefix => 'L';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new PlanId.
    /// </summary>
    public PlanId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a PlanId.
    /// </summary>
    public static PlanId Parse(string id) => Parse(id, v => new PlanId(v));

    /// <summary>
    /// Tries to parse a string ID into a PlanId.
    /// </summary>
    public static bool TryParse(string? id, out PlanId? result) => TryParse(id, v => new PlanId(v), out result);

    /// <summary>
    /// Creates a PlanId from a sequence value.
    /// </summary>
    public static PlanId FromSequence(long sequenceValue) => new(sequenceValue);
}
