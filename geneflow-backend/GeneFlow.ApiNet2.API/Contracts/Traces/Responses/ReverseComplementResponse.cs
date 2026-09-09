namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Reverse complement response.
/// </summary>
public sealed class ReverseComplementResponse
{
    public string TraceId { get; init; } = null!;
    public string OriginalSequence { get; init; } = null!;
    public string ReverseComplementSequence { get; init; } = null!;
    public int Length { get; init; }
}
