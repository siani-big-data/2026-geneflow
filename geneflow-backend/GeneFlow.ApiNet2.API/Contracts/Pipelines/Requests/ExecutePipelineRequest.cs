using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;

/// <summary>
/// Request model for executing a pipeline on a trace.
/// </summary>
public sealed record ExecutePipelineRequest
{
    /// <summary>The trace ID to execute the pipeline on.</summary>
    [Required]
    public required string TraceId { get; init; }
}
