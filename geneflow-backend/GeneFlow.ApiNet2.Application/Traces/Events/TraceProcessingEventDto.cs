namespace GeneFlow.ApiNet2.Application.Traces.Events;

/// <summary>
/// In-process notification carried over the SSE channel when a trace's
/// processing pipeline transitions state (started, completed, or failed).
/// Mirrors <see cref="GeneFlow.ApiNet2.Application.Analysis.Events.AnalysisEventDto"/>
/// but is keyed by trace id and consumed from the <c>traces</c> Redis stream
/// category.
/// </summary>
/// <param name="EventType">
/// Stable, frontend-friendly event name:
/// <c>trace.processing.started</c>, <c>trace.processing.completed</c>, or
/// <c>trace.processing.failed</c>.
/// </param>
/// <param name="TraceId">Identifier of the trace whose status changed.</param>
/// <param name="StudyId">Identifier of the parent study (may be null if not in payload).</param>
/// <param name="Success">True for completed, false for failed; true for started (no outcome yet).</param>
/// <param name="Error">Failure reason when <see cref="Success"/> is false; otherwise null.</param>
/// <param name="QualityScore">Average quality score; populated only on completion.</param>
/// <param name="OccurredAt">UTC timestamp the original domain event was produced.</param>
public sealed record TraceProcessingEventDto(
    string EventType,
    string TraceId,
    string? StudyId,
    bool Success,
    string? Error,
    double? QualityScore,
    DateTimeOffset OccurredAt);
