using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for changing study status.
/// </summary>
public sealed record ChangeStudyStatusRequest
{
    /// <summary>The new status ID. Valid values: 1=Draft, 2=Active, 3=Completed, 4=Published, 5=Archived.</summary>
    [Required]
    [Range(1, 5)]
    public required int NewStatusId { get; init; }
}
