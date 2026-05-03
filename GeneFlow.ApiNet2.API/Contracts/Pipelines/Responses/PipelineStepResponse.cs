namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;

/// <summary>
/// Response model for a pipeline step.
/// </summary>
public sealed record PipelineStepResponse(
    string Id,
    int StepTypeId,
    string StepTypeName,
    string StepTypeDisplayName,
    int Order,
    string? Label,
    string Configuration,
    bool IsEnabled,
    string DisplayName,
    DateTime CreatedAt);
