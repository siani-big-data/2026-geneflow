using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when multiple traces are uploaded in a batch.
/// </summary>
public sealed record TracesUploadedEvent(
    IReadOnlyList<TraceId> TraceIds,
    StudyId StudyId,
    int Count,
    UserId UploadedBy) : DomainEvent;
