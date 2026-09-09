namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;

/// <summary>
/// Response model for complete pipeline information.
/// </summary>
public sealed record PipelineResponse(
    string Id,
    string StudyId,
    string OwnerId,
    string Name,
    string? Description,
    int StatusId,
    string StatusName,
    bool CanBeEdited,
    bool CanBeExecuted,
    int StepCount,
    int EnabledStepCount,
    IReadOnlyList<PipelineStepResponse> Steps,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy);

/// <summary>
/// Response model for pipeline summary (list views).
/// </summary>
public sealed record PipelineSummaryResponse(
    string Id,
    string Name,
    string? Description,
    int StatusId,
    string StatusName,
    int StepCount,
    int EnabledStepCount,
    DateTime CreatedAt,
    DateTime? ModifiedAt);
