using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Traces.Events;

/// <summary>
/// Event raised when a user requests an analysis job on a trace (trimming,
/// heterozygote detection, motif search, translation, ORF detection,
/// restriction analysis, etc.). The activity projector maps this event to
/// the <c>Analysis</c> object type with the <c>Requested</c> verb so the
/// request is surfaced in the user's feed and the study timeline.
/// </summary>
public sealed record AnalysisRequestedEvent(
    TraceId TraceId,
    StudyId StudyId,
    string AnalysisType,
    UserId RequestedBy) : DomainEvent;
