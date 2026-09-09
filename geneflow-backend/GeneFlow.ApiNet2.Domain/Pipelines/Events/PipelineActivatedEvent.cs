using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Events;

/// <summary>
/// Event raised when a pipeline is activated.
/// </summary>
public sealed record PipelineActivatedEvent(
    PipelineId PipelineId,
    UserId ActivatedBy) : DomainEvent;
