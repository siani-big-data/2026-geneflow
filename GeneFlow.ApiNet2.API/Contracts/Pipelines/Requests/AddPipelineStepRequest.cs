using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;

/// <summary>
/// Request model for adding a step to a pipeline.
/// </summary>
public sealed record AddPipelineStepRequest
{
    /// <summary>The step type ID.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public required int StepTypeId { get; init; }

    /// <summary>Custom label for this step (optional, max 100 characters).</summary>
    [StringLength(100)]
    public string? Label { get; init; }

    /// <summary>JSON configuration for the step (optional).</summary>
    [StringLength(4000)]
    public string? Configuration { get; init; }

    /// <summary>Whether the step is enabled (default true).</summary>
    public bool IsEnabled { get; init; } = true;
}
