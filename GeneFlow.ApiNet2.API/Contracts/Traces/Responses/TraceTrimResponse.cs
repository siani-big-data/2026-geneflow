namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Response for a trace trim operation.
/// </summary>
public sealed class TraceTrimResponse
{
    public string Id { get; init; } = null!;
    public string TrimType { get; init; } = null!;
    public int TrimTypeId { get; init; }
    public string TrimEnd { get; init; } = null!;
    public int TrimEndId { get; init; }
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public int Length { get; init; }
    public string Algorithm { get; init; } = null!;
    public string? Reason { get; init; }
    public string AppliedBy { get; init; } = null!;
    public DateTime AppliedAt { get; init; }
    public bool IsActive { get; init; }
}
