namespace GeneFlow.ApiNet2.Application.Pipelines.Events;

/// <summary>
/// In-process notification carried over the SSE channel as a pipeline
/// execution progresses. Keyed by execution id and consumed from the
/// <c>pipelines</c> Redis stream category.
/// </summary>
/// <param name="EventType">
/// Stable, frontend-friendly event name:
/// <c>execution.started</c>, <c>execution.step.completed</c>,
/// <c>execution.completed</c>, or <c>execution.failed</c>.
/// </param>
/// <param name="ExecutionId">Identifier of the running pipeline execution.</param>
/// <param name="PipelineId">Identifier of the parent pipeline definition (nullable on step events).</param>
/// <param name="TraceId">Identifier of the trace being processed (nullable on step events).</param>
/// <param name="CompletedSteps">Number of completed steps so far (terminal events only; null otherwise).</param>
/// <param name="TotalSteps">Total number of steps in the pipeline (started/terminal; null on step events).</param>
/// <param name="StepType">Step type/key on <c>execution.step.completed</c>; null otherwise.</param>
/// <param name="Success">True for completed/started/step.completed-success; false for failed.</param>
/// <param name="Error">Failure reason when <see cref="Success"/> is false; otherwise null.</param>
/// <param name="OccurredAt">UTC timestamp the original domain event was produced.</param>
public sealed record PipelineExecutionEventDto(
    string EventType,
    string ExecutionId,
    string? PipelineId,
    string? TraceId,
    int? CompletedSteps,
    int? TotalSteps,
    string? StepType,
    bool Success,
    string? Error,
    DateTimeOffset OccurredAt);
