namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Response containing a paginated section of trace sequence data.
/// </summary>
public sealed class SequencePageResponse
{
    public string TraceId { get; init; } = null!;
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalBases { get; init; }
    public int TotalPages { get; init; }
    public string Bases { get; init; } = null!;
    public IReadOnlyList<int>? QualityScores { get; init; }
    public ChromatogramDataResponse? Chromatogram { get; init; }
}

/// <summary>
/// Chromatogram data for a sequence page.
/// </summary>
public sealed class ChromatogramDataResponse
{
    public IReadOnlyList<int>? AChannel { get; init; }
    public IReadOnlyList<int>? TChannel { get; init; }
    public IReadOnlyList<int>? GChannel { get; init; }
    public IReadOnlyList<int>? CChannel { get; init; }
    public IReadOnlyList<int>? PeakPositions { get; init; }
}
