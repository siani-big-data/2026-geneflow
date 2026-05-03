using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Events;

/// <summary>
/// Event raised when a pipeline execution fails.
/// </summary>
public sealed record PipelineExecutionFailedEvent(
    PipelineExecutionId ExecutionId,
    PipelineId PipelineId,
    TraceId TraceId,
    int CompletedSteps,
    string? ErrorMessage) : DomainEvent;
