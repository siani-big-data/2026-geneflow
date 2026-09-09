using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Events;

/// <summary>
/// Event raised when a pipeline is archived.
/// </summary>
public sealed record PipelineArchivedEvent(
    PipelineId PipelineId,
    UserId ArchivedBy) : DomainEvent;
