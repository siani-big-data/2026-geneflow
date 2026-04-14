using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.Events;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a trace is archived.
/// </summary>
public sealed record TraceArchivedEvent(
    TraceId TraceId,
    UserId ArchivedBy) : IDomainEvent;
