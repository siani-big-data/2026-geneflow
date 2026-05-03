using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Events;

/// <summary>
/// Event raised when a pipeline step execution completes.
/// </summary>
public sealed record PipelineStepCompletedEvent(
    PipelineExecutionId ExecutionId,
    Guid StepExecutionId,
    StepType StepType,
    int Order,
    bool IsSuccess,
    TimeSpan Duration) : DomainEvent;
