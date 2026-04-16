using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a trace is archived.
/// </summary>
public sealed record TraceArchivedEvent(
    TraceId TraceId,
    UserId ArchivedBy) : DomainEvent;
