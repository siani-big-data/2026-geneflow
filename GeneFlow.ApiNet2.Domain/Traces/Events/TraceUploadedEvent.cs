using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a new trace is uploaded.
/// </summary>
public sealed record TraceUploadedEvent(
    TraceId TraceId,
    StudyId StudyId,
    string FileName,
    TraceFormat Format,
    UserId UploadedBy) : DomainEvent;
