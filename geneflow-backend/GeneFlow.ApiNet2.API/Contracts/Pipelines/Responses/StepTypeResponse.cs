namespace GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;

/// <summary>
/// Response model for a step type.
/// </summary>
public sealed record StepTypeResponse(
    int Id,
    string Name,
    string DisplayName,
    string AnalysisKey,
    bool RequiresConfiguration,
    string ConfigurationSchema);
