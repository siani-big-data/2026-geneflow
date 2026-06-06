using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Orgs;

/// <summary>
/// Strongly-typed identifier for an Org aggregate.
/// Format: O######## (eight zero-padded digits).
/// </summary>
public sealed class OrgId : PrefixedId<OrgId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "orgs";

    /// <inheritdoc />
    protected override char Prefix => 'O';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    public OrgId(long value) : base(value) { }

    public static OrgId Parse(string id) => Parse(id, v => new OrgId(v));

    public static bool TryParse(string? id, out OrgId? result)
        => TryParse(id, v => new OrgId(v), out result);

    public static OrgId FromSequence(long sequenceValue) => new(sequenceValue);
}
