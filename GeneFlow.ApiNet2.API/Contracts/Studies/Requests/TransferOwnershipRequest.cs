using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for transferring study ownership.
/// </summary>
public sealed record TransferOwnershipRequest
{
    /// <summary>The user ID of the new owner (must be an admin of the study).</summary>
    [Required]
    public required string NewOwnerId { get; init; }
}
