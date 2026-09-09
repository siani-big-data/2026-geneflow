using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Events;

/// <summary>
/// Event raised when a new pipeline is created.
/// </summary>
public sealed record PipelineCreatedEvent(
    PipelineId PipelineId,
    StudyId StudyId,
    string Name,
    UserId CreatedBy) : DomainEvent;
