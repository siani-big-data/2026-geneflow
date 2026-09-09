using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Pipelines;

/// <summary>
/// Strongly-typed identifier for PipelineExecution entity.
/// </summary>
public sealed class PipelineExecutionId : PrefixedId<PipelineExecutionId>
{
    /// <summary>
    /// Sequence name for ID generation.
    /// </summary>
    public const string SequenceName = "pipeline_executions";

    /// <inheritdoc />
    protected override char Prefix => 'X';

    /// <inheritdoc />
    protected override int NumericLength => 8;

    /// <summary>
    /// Initializes a new PipelineExecutionId.
    /// </summary>
    public PipelineExecutionId(long value) : base(value) { }

    /// <summary>
    /// Parses a string ID into a PipelineExecutionId.
    /// </summary>
    public static PipelineExecutionId Parse(string id) => Parse(id, v => new PipelineExecutionId(v));

    /// <summary>
    /// Tries to parse a string ID into a PipelineExecutionId.
    /// </summary>
    public static bool TryParse(string? id, out PipelineExecutionId? result) => TryParse(id, v => new PipelineExecutionId(v), out result);

    /// <summary>
    /// Creates a PipelineExecutionId from a sequence value.
    /// </summary>
    public static PipelineExecutionId FromSequence(long sequenceValue) => new(sequenceValue);
}
