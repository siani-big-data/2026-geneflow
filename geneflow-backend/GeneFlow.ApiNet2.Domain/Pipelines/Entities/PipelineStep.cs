using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Entities;

/// <summary>
/// Represents a step within a pipeline.
/// </summary>
public sealed class PipelineStep : Entity<Guid>
{
    public const int MaxLabelLength = 100;

    public StepType StepType { get; private set; } = null!;
    public int Order { get; private set; }
    public string? Label { get; private set; }
    public StepConfiguration Configuration { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PipelineStep() : base() { }

    private PipelineStep(
        Guid id,
        StepType stepType,
        int order,
        string? label,
        StepConfiguration configuration,
        bool isEnabled) : base(id)
    {
        StepType = stepType;
        Order = order;
        Label = label;
        Configuration = configuration;
        IsEnabled = isEnabled;
        CreatedAt = DateTime.UtcNow;
    }

    internal static PipelineStep Create(
        StepType stepType,
        int order,
        string? label,
        StepConfiguration configuration,
        bool isEnabled = true)
    {
        return new PipelineStep(
            Guid.NewGuid(),
            stepType,
            order,
            label?.Trim(),
            configuration,
            isEnabled);
    }

    internal void UpdateConfiguration(StepConfiguration configuration)
    {
        Configuration = configuration;
    }

    internal void UpdateLabel(string? label)
    {
        Label = label?.Trim();
    }

    internal void SetOrder(int order)
    {
        Order = order;
    }

    internal void Enable()
    {
        IsEnabled = true;
    }

    internal void Disable()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// Gets the display name for this step (label or step type name).
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? StepType.DisplayName : Label;
}
