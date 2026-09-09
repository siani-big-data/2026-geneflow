using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Pipelines;

/// <summary>
/// Strongly-typed identifier for Pipeline aggregate.
/// </summary>
public sealed class PipelineId : PrefixedId<PipelineId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "pipelines";

    /// <inheritdoc />
    protected override char Prefix => 'P';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new PipelineId.
    /// </summary>
    public PipelineId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a PipelineId.
    /// </summary>
    public static PipelineId Parse(string id) => Parse(id, v => new PipelineId(v));

    /// <summary>
    /// Tries to parse a string ID into a PipelineId.
    /// </summary>
    public static bool TryParse(string? id, out PipelineId? result) => TryParse(id, v => new PipelineId(v), out result);

    /// <summary>
    /// Creates a PipelineId from a sequence value.
    /// </summary>
    public static PipelineId FromSequence(long sequenceValue) => new(sequenceValue);
}
