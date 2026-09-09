using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Analysis;

/// <summary>
/// Error definitions for Alignment operations.
/// </summary>
public static class AlignmentErrors
{
    public static Error InvalidTraceId(string traceId) => Error.Validation(
        "Alignment.InvalidTraceId",
        $"The trace ID '{traceId}' format is invalid.");

    public static Error TraceNotFound(string traceId) => Error.NotFound(
        "Alignment.TraceNotFound",
        $"Trace '{traceId}' was not found.");

    public static Error TraceNotProcessed(string traceId) => Error.Failure(
        "Alignment.TraceNotProcessed",
        $"Trace '{traceId}' must be processed before alignment.");

    public static readonly Error InsufficientTraces = Error.Validation(
        "Alignment.InsufficientTraces",
        "At least 2 traces are required for alignment.");

    public static Error InvalidAlignmentType(string type) => Error.Validation(
        "Alignment.InvalidAlignmentType",
        $"'{type}' is not a valid alignment type. Valid types: pairwise, multiple.");
}
