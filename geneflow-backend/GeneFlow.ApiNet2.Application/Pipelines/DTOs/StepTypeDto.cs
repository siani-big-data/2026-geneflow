namespace GeneFlow.ApiNet2.Application.Pipelines.DTOs;

/// <summary>
/// Step type data transfer object.
/// </summary>
public sealed record StepTypeDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string AnalysisKey { get; init; }
    public required bool RequiresConfiguration { get; init; }
    public required string ConfigurationSchema { get; init; }
}
