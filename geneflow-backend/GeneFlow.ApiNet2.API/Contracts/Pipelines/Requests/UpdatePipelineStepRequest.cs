using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;

/// <summary>
/// Request model for updating a pipeline step.
/// </summary>
public sealed record UpdatePipelineStepRequest
{
    /// <summary>Custom label for this step (optional, max 100 characters).</summary>
    [StringLength(100)]
    public string? Label { get; init; }

    /// <summary>JSON configuration for the step (optional).</summary>
    [StringLength(4000)]
    public string? Configuration { get; init; }

    /// <summary>Whether the step is enabled.</summary>
    [Required]
    public required bool IsEnabled { get; init; }
}
