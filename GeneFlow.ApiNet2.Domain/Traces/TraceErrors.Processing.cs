using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// Errors related to trace status transitions, processing lifecycle and
/// pagination of analysis results. Partial of <see cref="TraceErrors"/>.
/// </summary>
public static partial class TraceErrors
{
    /// <summary>Invalid trace status.</summary>
    public static readonly Error InvalidStatus = Error.Validation(
        "Trace.InvalidStatus", "Invalid trace status.");

    /// <summary>Invalid status transition.</summary>
    public static Error InvalidStatusTransition(TraceStatus from, TraceStatus to) =>
        Error.Validation("Trace.InvalidStatusTransition", $"Cannot transition from {from.Name} to {to.Name}.");

    /// <summary>Trace cannot be edited in its current status.</summary>
    public static readonly Error CannotEditInCurrentStatus = Error.Validation(
        "Trace.CannotEditInCurrentStatus", "Trace cannot be edited in its current status.");

    /// <summary>Trace cannot be deleted while processing.</summary>
    public static readonly Error CannotDeleteWhileProcessing = Error.Validation(
        "Trace.CannotDeleteWhileProcessing", "Trace cannot be deleted while processing.");

    /// <summary>Only failed traces can be retried.</summary>
    public static readonly Error CannotRetryNotFailed = Error.Validation(
        "Trace.CannotRetryNotFailed", "Only failed traces can be retried.");

    /// <summary>Trace is already being processed.</summary>
    public static readonly Error AlreadyProcessing = Error.Conflict(
        "Trace.AlreadyProcessing", "Trace is already being processed.");

    /// <summary>Trace must be uploaded before processing.</summary>
    public static readonly Error NotUploaded = Error.Validation(
        "Trace.NotUploaded", "Trace must be in Uploaded status to start processing.");

    /// <summary>Trace is not currently being processed.</summary>
    public static readonly Error NotProcessing = Error.Validation(
        "Trace.NotProcessing", "Trace is not currently being processed.");

    /// <summary>Failure reason is required.</summary>
    public static readonly Error FailureReasonRequired = Error.Validation(
        "Trace.FailureReasonRequired", "Failure reason is required.");

    /// <summary>Failure reason is too long.</summary>
    public static Error FailureReasonTooLong(int max) => Error.Validation(
        "Trace.FailureReasonTooLong", $"Failure reason must not exceed {max} characters.");

    /// <summary>Invalid page number.</summary>
    public static readonly Error InvalidPageNumber = Error.Validation(
        "Trace.InvalidPageNumber", "Page number must be 1 or greater.");

    /// <summary>Invalid page size.</summary>
    public static readonly Error InvalidPageSize = Error.Validation(
        "Trace.InvalidPageSize", "Page size must be between 100 and 50,000 bases.");

    /// <summary>Page is out of range.</summary>
    public static readonly Error PageOutOfRange = Error.Validation(
        "Trace.PageOutOfRange", "Requested page is beyond the available sequence data.");

    /// <summary>Chunked sequence data is not available.</summary>
    public static readonly Error ChunkedDataNotAvailable = Error.NotFound(
        "Trace.ChunkedDataNotAvailable", "Chunked sequence data is not available for this trace.");

    /// <summary>Analysis result was not found.</summary>
    public static readonly Error AnalysisResultNotFound = Error.NotFound(
        "Trace.AnalysisResultNotFound", "Analysis result was not found.");

    /// <summary>Analysis result of given type was not found.</summary>
    public static Error AnalysisTypeNotFound(string type) => Error.NotFound(
        "Trace.AnalysisTypeNotFound", $"Analysis result of type '{type}' was not found.");
}
