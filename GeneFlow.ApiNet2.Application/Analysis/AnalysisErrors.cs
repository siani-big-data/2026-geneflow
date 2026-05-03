using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Analysis;

/// <summary>
/// Error definitions for Analysis operations.
/// </summary>
public static class AnalysisErrors
{
    public static readonly Error InvalidTraceId = Error.Validation(
        "Analysis.InvalidTraceId",
        "The trace ID format is invalid.");

    public static Error TraceNotFound(string traceId) => Error.NotFound(
        "Analysis.TraceNotFound",
        $"Trace '{traceId}' was not found.");

    public static readonly Error TraceNotProcessed = Error.Failure(
        "Analysis.TraceNotProcessed",
        "The trace must be processed before running analysis.");

    public static Error InvalidAnalysisType(string type) => Error.Validation(
        "Analysis.InvalidAnalysisType",
        $"'{type}' is not a valid analysis type. Valid types: quality, trimming, heterozygote, motif, translation, orf, restriction.");

    public static readonly Error MotifPatternRequired = Error.Validation(
        "Analysis.MotifPatternRequired",
        "A pattern is required for motif search.");
}
