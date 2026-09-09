namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Trim region data transfer object.
/// </summary>
public sealed record TrimRegionDto
{
    public required int Start5Prime { get; init; }
    public required int End5Prime { get; init; }
    public required int Start3Prime { get; init; }
    public required int End3Prime { get; init; }
    public required string Algorithm { get; init; }
    public required string TrimmedBy { get; init; }
    public required DateTime TrimmedAt { get; init; }

    // Computed
    public int TrimmedLength => Start3Prime - End5Prime;
    public int TotalBasesTrimmed => End5Prime + (End3Prime - Start3Prime);
}
