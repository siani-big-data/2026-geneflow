using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Events;

/// <summary>
/// Event raised when a pipeline execution completes successfully.
/// </summary>
public sealed record PipelineExecutionCompletedEvent(
    PipelineExecutionId ExecutionId,
    PipelineId PipelineId,
    TraceId TraceId,
    int CompletedSteps,
    TimeSpan Duration) : DomainEvent;
