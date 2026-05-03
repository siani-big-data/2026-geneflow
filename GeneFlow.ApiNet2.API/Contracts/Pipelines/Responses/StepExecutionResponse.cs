namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;

/// <summary>
/// Response model for a step execution.
/// </summary>
public sealed record StepExecutionResponse(
    string Id,
    string PipelineStepId,
    int Order,
    int StepTypeId,
    string StepTypeName,
    string StepTypeDisplayName,
    int StatusId,
    string StatusName,
    bool IsInProgress,
    bool IsSuccess,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? DurationSeconds,
    string? ErrorMessage,
    string? ResultSummary,
    string? ResultData);
