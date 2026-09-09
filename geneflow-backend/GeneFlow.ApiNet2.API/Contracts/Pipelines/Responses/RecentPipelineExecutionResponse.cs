namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;

/// <summary>
/// Response model for the dashboard's "recent pipelines" lateral panel.
/// </summary>
public sealed record RecentPipelineExecutionResponse(
    string Id,
    string PipelineId,
    string PipelineName,
    string TraceId,
    string StudyId,
    string StudyTitle,
    int StatusId,
    string StatusName,
    int TotalSteps,
    int CompletedSteps,
    int ProgressPercentage,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    double? DurationSeconds);
