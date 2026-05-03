namespace GeneFlow.ApiNet2.Application.Pipelines.DTOs;

/// <summary>
/// Step execution data transfer object.
/// </summary>
public sealed record StepExecutionDto
{
    public required string Id { get; init; }
    public required string PipelineStepId { get; init; }
    public required int Order { get; init; }
    public required int StepTypeId { get; init; }
    public required string StepTypeName { get; init; }
    public required string StepTypeDisplayName { get; init; }
    public required int StatusId { get; init; }
    public required string StatusName { get; init; }
    public required bool IsInProgress { get; init; }
    public required bool IsSuccess { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public double? DurationSeconds { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ResultSummary { get; init; }
    public string? ResultData { get; init; }
}
