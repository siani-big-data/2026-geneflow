namespace GeneFlow.ApiNet2.Application.Pipelines.DTOs;

/// <summary>
/// Pipeline step data transfer object.
/// </summary>
public sealed record PipelineStepDto
{
    public required string Id { get; init; }
    public required int StepTypeId { get; init; }
    public required string StepTypeName { get; init; }
    public required string StepTypeDisplayName { get; init; }
    public required int Order { get; init; }
    public string? Label { get; init; }
    public required string Configuration { get; init; }
    public required bool IsEnabled { get; init; }
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// The display name for this step (label or step type name).
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? StepTypeDisplayName : Label;
}
