using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for updating a study's README markdown.
/// </summary>
public sealed record UpdateReadmeRequest
{
    /// <summary>The README markdown content (optional, max 100_000 characters).</summary>
    [StringLength(100_000)]
    public string? Markdown { get; init; }
}
