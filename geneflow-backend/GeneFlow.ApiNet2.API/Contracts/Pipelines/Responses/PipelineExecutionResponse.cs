namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;

/// <summary>
/// Response model for complete pipeline execution information.
/// </summary>
public sealed record PipelineExecutionResponse(
    string Id,
    string PipelineId,
    string PipelineName,
    string TraceId,
    string StartedById,
    int StatusId,
    string StatusName,
    bool IsInProgress,
    bool IsTerminal,
    int TotalSteps,
    int CompletedSteps,
    int ProgressPercentage,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? DurationSeconds,
    IReadOnlyList<StepExecutionResponse> StepExecutions);

/// <summary>
/// Response model for pipeline execution summary (list views).
/// </summary>
public sealed record PipelineExecutionSummaryResponse(
    string Id,
    string PipelineId,
    string PipelineName,
    string TraceId,
    int StatusId,
    string StatusName,
    int TotalSteps,
    int CompletedSteps,
    int ProgressPercentage,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    double? DurationSeconds);
