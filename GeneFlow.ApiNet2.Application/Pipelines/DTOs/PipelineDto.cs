namespace GeneFlow.ApiNet2.Application.Pipelines.DTOs;

/// <summary>
/// Full pipeline data transfer object.
/// </summary>
public sealed record PipelineDto
{
    public required string Id { get; init; }
    public required string StudyId { get; init; }
    public required string OwnerId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int StatusId { get; init; }
    public required string StatusName { get; init; }
    public required bool CanBeEdited { get; init; }
    public required bool CanBeExecuted { get; init; }
    public required int StepCount { get; init; }
    public required int EnabledStepCount { get; init; }
    public required IReadOnlyList<PipelineStepDto> Steps { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}

/// <summary>
/// Summary pipeline data transfer object for listings.
/// </summary>
public sealed record PipelineSummaryDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int StatusId { get; init; }
    public required string StatusName { get; init; }
    public required int StepCount { get; init; }
    public required int EnabledStepCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
