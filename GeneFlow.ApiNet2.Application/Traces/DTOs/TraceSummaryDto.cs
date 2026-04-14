namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Lightweight trace summary for list views.
/// </summary>
public sealed record TraceSummaryDto
{
    public required string Id { get; init; }
    public required string StudyId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    // File info
    public required string FileName { get; init; }
    public required long SizeBytes { get; init; }

    // Format and status
    public required string Format { get; init; }
    public required int FormatId { get; init; }
    public required string Status { get; init; }
    public required int StatusId { get; init; }

    // Key quality metric
    public decimal? AverageQualityScore { get; init; }
    public int? TotalBases { get; init; }

    // Processing info
    public required bool HasChromatogramData { get; init; }
    public DateTime? ProcessedAt { get; init; }

    // Audit
    public required DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
