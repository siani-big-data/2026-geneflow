namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Trace counts by status for a study.
/// </summary>
public sealed record TraceCountsDto
{
    public required int Total { get; init; }
    public required int Uploaded { get; init; }
    public required int Validating { get; init; }
    public required int Processing { get; init; }
    public required int Processed { get; init; }
    public required int Failed { get; init; }
    public required int Archived { get; init; }
}
