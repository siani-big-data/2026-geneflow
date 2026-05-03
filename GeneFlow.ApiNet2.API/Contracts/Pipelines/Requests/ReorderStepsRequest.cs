using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;

/// <summary>
/// Request model for reordering pipeline steps.
/// </summary>
public sealed record ReorderStepsRequest
{
    /// <summary>The step IDs in the desired order.</summary>
    [Required]
    [MinLength(1)]
    public required List<string> StepIds { get; init; }
}
