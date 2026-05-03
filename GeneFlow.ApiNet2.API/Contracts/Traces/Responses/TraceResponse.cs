namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Full trace response.
/// </summary>
public sealed class TraceResponse
{
    public string Id { get; init; } = null!;
    public string StudyId { get; init; } = null!;
    public string UploadedBy { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long SizeBytes { get; init; }
    public string Format { get; init; } = null!;
    public int FormatId { get; init; }
    public string Status { get; init; } = null!;
    public int StatusId { get; init; }
    public QualityMetricsResponse? QualityMetrics { get; init; }
    public IReadOnlyList<TraceTrimResponse> Trims { get; init; } = [];
    public int ActiveTrimCount { get; init; }
    public bool HasChromatogramData { get; init; }
    public string? FailureReason { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public int ActiveEditCount { get; init; }
    public int AnnotationCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}
