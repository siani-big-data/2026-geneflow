namespace GeneFlow.ApiNet2.Application.Pipelines.DTOs;

/// <summary>
/// Pipeline execution data transfer object.
/// </summary>
public sealed record PipelineExecutionDto
{
    public required string Id { get; init; }
    public required string PipelineId { get; init; }
    public required string PipelineName { get; init; }
    public required string TraceId { get; init; }
    public required string StartedById { get; init; }
    public required int StatusId { get; init; }
    public required string StatusName { get; init; }
    public required bool IsInProgress { get; init; }
    public required bool IsTerminal { get; init; }
    public required int TotalSteps { get; init; }
    public required int CompletedSteps { get; init; }
    public required int ProgressPercentage { get; init; }
    public string? ErrorMessage { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public double? DurationSeconds { get; init; }
    public required IReadOnlyList<StepExecutionDto> StepExecutions { get; init; }
}

/// <summary>
/// Summary execution data transfer object for listings.
/// </summary>
public sealed record PipelineExecutionSummaryDto
{
    public required string Id { get; init; }
    public required string PipelineId { get; init; }
    public required string PipelineName { get; init; }
    public required string TraceId { get; init; }
    public required int StatusId { get; init; }
    public required string StatusName { get; init; }
    public required int TotalSteps { get; init; }
    public required int CompletedSteps { get; init; }
    public required int ProgressPercentage { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public double? DurationSeconds { get; init; }
}
