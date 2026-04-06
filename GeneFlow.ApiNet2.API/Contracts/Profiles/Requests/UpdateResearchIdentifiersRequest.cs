using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Profiles.Requests;

/// <summary>
/// Request model for updating research identifiers (ORCID, website).
/// </summary>
public sealed record UpdateResearchIdentifiersRequest
{
    /// <summary>
    /// The ORCID ID (optional). Format: 0000-0000-0000-0000 or 0000-0000-0000-000X.
    /// </summary>
    [RegularExpression(@"^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$",
        ErrorMessage = "ORCID ID must be in format: 0000-0000-0000-0000")]
    public string? OrcidId { get; init; }

    /// <summary>The personal/professional website URL (optional).</summary>
    [Url]
    [StringLength(500)]
    public string? Website { get; init; }
}
