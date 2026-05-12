namespace GeneFlow.ApiNet2.Application.Pipelines.Queries.GetRecentUserExecutions;

/// <summary>
/// Pipeline execution summary for the dashboard's "recent pipelines" lateral panel.
/// Includes study identity so the UI can deep-link to the originating study.
/// </summary>
public sealed record RecentPipelineExecutionDto
{
    public required string Id { get; init; }
    public required string PipelineId { get; init; }
    public required string PipelineName { get; init; }
    public required string TraceId { get; init; }
    public required string StudyId { get; init; }
    public required string StudyTitle { get; init; }
    public required int StatusId { get; init; }
    public required string StatusName { get; init; }
    public required int TotalSteps { get; init; }
    public required int CompletedSteps { get; init; }
    public required int ProgressPercentage { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public double? DurationSeconds { get; init; }
}
