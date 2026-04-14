namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Full trace data transfer object.
/// </summary>
public sealed record TraceDto
{
    public required string Id { get; init; }
    public required string StudyId { get; init; }
    public required string UploadedBy { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    // File info
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }

    // Format and status
    public required string Format { get; init; }
    public required int FormatId { get; init; }
    public required string Status { get; init; }
    public required int StatusId { get; init; }

    // Quality metrics (nullable before processing)
    public QualityMetricsDto? QualityMetrics { get; init; }

    // Trim info (nullable)
    public TrimRegionDto? TrimRegion { get; init; }

    // Processing info
    public required bool HasChromatogramData { get; init; }
    public string? FailureReason { get; init; }
    public DateTime? ProcessedAt { get; init; }

    // Edits and annotations counts
    public required int ActiveEditCount { get; init; }
    public required int AnnotationCount { get; init; }

    // Audit
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}
