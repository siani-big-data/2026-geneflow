using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Profiles.Requests;

/// <summary>
/// Request model for fetching multiple profiles by user IDs.
/// </summary>
public sealed record GetProfilesByUserIdsRequest
{
    /// <summary>The list of user IDs to fetch profiles for.</summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one user ID is required")]
    [MaxLength(100, ErrorMessage = "Cannot request more than 100 profiles at once")]
    public required IReadOnlyList<string> UserIds { get; init; }
}
