namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Data transfer object for a trace trim operation.
/// </summary>
public sealed record TraceTrimDto
{
    public required string Id { get; init; }
    public required string TrimType { get; init; }
    public required int TrimTypeId { get; init; }
    public required string TrimEnd { get; init; }
    public required int TrimEndId { get; init; }
    public required int StartPosition { get; init; }
    public required int EndPosition { get; init; }
    public required int Length { get; init; }
    public required string Algorithm { get; init; }
    public string? Reason { get; init; }
    public required string AppliedBy { get; init; }
    public required DateTime AppliedAt { get; init; }
    public required bool IsActive { get; init; }
}
