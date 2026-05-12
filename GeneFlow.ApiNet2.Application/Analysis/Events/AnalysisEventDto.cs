namespace GeneFlow.ApiNet2.Application.Analysis.Events;

/// <summary>
/// In-process notification carried over the SSE channel when an analysis
/// step completes (successfully or not) for a given trace.
/// </summary>
/// <param name="EventType">Raw event type from the Python worker (e.g. <c>TrimmingCompleted</c>).</param>
/// <param name="AnalysisKey">Stable analysis key (e.g. <c>trimming</c>, <c>motif</c>).</param>
/// <param name="Success">True for <c>*Completed</c>, false for <c>*Failed</c>.</param>
/// <param name="Error">Error message when <see cref="Success"/> is false; otherwise null.</param>
/// <param name="OccurredAt">UTC timestamp the event was produced by the worker.</param>
public sealed record AnalysisEventDto(
    string EventType,
    string AnalysisKey,
    bool Success,
    string? Error,
    DateTimeOffset OccurredAt);
