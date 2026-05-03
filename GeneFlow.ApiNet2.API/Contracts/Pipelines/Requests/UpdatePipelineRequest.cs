using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;

/// <summary>
/// Request model for updating a pipeline.
/// </summary>
public sealed record UpdatePipelineRequest
{
    /// <summary>The pipeline name (3-100 characters).</summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public required string Name { get; init; }

    /// <summary>The pipeline description (optional, max 1000 characters).</summary>
    [StringLength(1000)]
    public string? Description { get; init; }
}
