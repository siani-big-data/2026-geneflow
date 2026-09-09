namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Trace summary response for list views.
/// </summary>
public sealed class TraceSummaryResponse
{
    public string Id { get; init; } = null!;
    public string StudyId { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string FileName { get; init; } = null!;
    public long SizeBytes { get; init; }
    public string Format { get; init; } = null!;
    public int FormatId { get; init; }
    public string Status { get; init; } = null!;
    public int StatusId { get; init; }
    public decimal? AverageQualityScore { get; init; }
    public int? TotalBases { get; init; }
    public bool HasChromatogramData { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
