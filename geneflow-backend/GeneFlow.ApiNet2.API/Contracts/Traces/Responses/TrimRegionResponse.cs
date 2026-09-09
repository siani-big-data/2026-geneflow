namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Trim region response.
/// </summary>
public sealed class TrimRegionResponse
{
    public int Start5Prime { get; init; }
    public int End5Prime { get; init; }
    public int Start3Prime { get; init; }
    public int End3Prime { get; init; }
    public string Algorithm { get; init; } = null!;
    public string? TrimmedBy { get; init; }
    public DateTime TrimmedAt { get; init; }
    public int TrimmedLength => Start3Prime - End5Prime;
}
