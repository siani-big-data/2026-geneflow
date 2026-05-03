using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Events;

/// <summary>
/// Event raised when a step is added to a pipeline.
/// </summary>
public sealed record PipelineStepAddedEvent(
    PipelineId PipelineId,
    Guid StepId,
    StepType StepType,
    int Order) : DomainEvent;
