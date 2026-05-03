using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Events;

/// <summary>
/// Event raised when a pipeline execution starts.
/// </summary>
public sealed record PipelineExecutionStartedEvent(
    PipelineExecutionId ExecutionId,
    PipelineId PipelineId,
    TraceId TraceId,
    UserId StartedBy,
    int TotalSteps) : DomainEvent;
