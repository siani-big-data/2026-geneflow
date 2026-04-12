using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for updating a study.
/// </summary>
public sealed record UpdateStudyRequest
{
    /// <summary>The study title (5-200 characters).</summary>
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public required string Title { get; init; }

    /// <summary>The study description (optional, max 5000 characters).</summary>
    [StringLength(5000)]
    public string? Description { get; init; }

    /// <summary>The research field ID.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public required int ResearchFieldId { get; init; }

    /// <summary>The institution name (optional, max 200 characters).</summary>
    [StringLength(200)]
    public string? Institution { get; init; }

    /// <summary>The principal investigator name (optional, max 200 characters).</summary>
    [StringLength(200)]
    public string? PrincipalInvestigator { get; init; }

    /// <summary>Tags for the study (optional, max 10 tags).</summary>
    [MaxLength(10)]
    public List<string>? Tags { get; init; }
}
