using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for adding a paper to a study.
/// Note: File upload should be done via multipart/form-data.
/// </summary>
public sealed record AddStudyPaperRequest
{
    /// <summary>The paper title (required, max 500 characters).</summary>
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Title { get; init; }

    /// <summary>The paper authors (optional).</summary>
    [StringLength(2000)]
    public string? Authors { get; init; }

    /// <summary>The paper DOI (optional, max 100 characters).</summary>
    [StringLength(100)]
    public string? Doi { get; init; }

    /// <summary>The paper abstract (optional).</summary>
    [StringLength(5000)]
    public string? Abstract { get; init; }

    /// <summary>The journal name (optional, max 200 characters).</summary>
    [StringLength(200)]
    public string? Journal { get; init; }

    /// <summary>The publication year (optional).</summary>
    [Range(1900, 2100)]
    public int? PublicationYear { get; init; }
}
