using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// Contains all domain errors related to the Trace aggregate.
/// </summary>
public static class TraceErrors
{
    // General errors
    public static readonly Error NotFound = Error.NotFound("Trace.NotFound", "Trace was not found.");
    public static Error TraceNotFoundById(string id) => Error.NotFound("Trace.NotFoundById", $"Trace with ID '{id}' was not found.");
    public static readonly Error InvalidTraceId = Error.Validation("Trace.InvalidTraceId", "Invalid trace ID.");
    public static readonly Error InvalidUserId = Error.Validation("Trace.InvalidUserId", "Invalid user ID.");
    public static readonly Error InvalidStudyId = Error.Validation("Trace.InvalidStudyId", "Invalid study ID.");

    // Name errors
    public static readonly Error NameRequired = Error.Validation("Trace.NameRequired", "Trace name is required.");
    public static Error NameTooShort(int min) => Error.Validation("Trace.NameTooShort", $"Trace name must be at least {min} characters.");
    public static Error NameTooLong(int max) => Error.Validation("Trace.NameTooLong", $"Trace name must not exceed {max} characters.");

    // Description errors
    public static Error DescriptionTooLong(int max) => Error.Validation("Trace.DescriptionTooLong", $"Trace description must not exceed {max} characters.");

    // File errors
    public static readonly Error FileNameRequired = Error.Validation("Trace.FileNameRequired", "File name is required.");
    public static Error FileNameTooLong(int max) => Error.Validation("Trace.FileNameTooLong", $"File name must not exceed {max} characters.");
    public static readonly Error ContentTypeRequired = Error.Validation("Trace.ContentTypeRequired", "Content type is required.");
    public static Error ContentTypeTooLong(int max) => Error.Validation("Trace.ContentTypeTooLong", $"Content type must not exceed {max} characters.");
    public static readonly Error StoragePathRequired = Error.Validation("Trace.StoragePathRequired", "Storage path is required.");
    public static Error StoragePathTooLong(int max) => Error.Validation("Trace.StoragePathTooLong", $"Storage path must not exceed {max} characters.");
    public static readonly Error InvalidFileSize = Error.Validation("Trace.InvalidFileSize", "File size must be greater than zero.");
    public static Error FileTooLarge(long maxBytes) => Error.Validation("Trace.FileTooLarge", $"File size exceeds the maximum allowed size of {maxBytes / (1024 * 1024)} MB.");
    public static readonly Error ChecksumRequired = Error.Validation("Trace.ChecksumRequired", "Checksum is required.");
    public static Error ChecksumTooLong(int max) => Error.Validation("Trace.ChecksumTooLong", $"Checksum must not exceed {max} characters.");
    public static readonly Error UnsupportedFormat = Error.Validation("Trace.UnsupportedFormat", "Unsupported trace file format.");

    // Status errors
    public static readonly Error InvalidStatus = Error.Validation("Trace.InvalidStatus", "Invalid trace status.");
    public static Error InvalidStatusTransition(TraceStatus from, TraceStatus to) =>
        Error.Validation("Trace.InvalidStatusTransition", $"Cannot transition from {from.Name} to {to.Name}.");
    public static readonly Error CannotEditInCurrentStatus = Error.Validation("Trace.CannotEditInCurrentStatus", "Trace cannot be edited in its current status.");
    public static readonly Error CannotDeleteWhileProcessing = Error.Validation("Trace.CannotDeleteWhileProcessing", "Trace cannot be deleted while processing.");
    public static readonly Error CannotRetryNotFailed = Error.Validation("Trace.CannotRetryNotFailed", "Only failed traces can be retried.");

    // Permission errors
    public static readonly Error InsufficientPermissions = Error.Forbidden("Trace.InsufficientPermissions", "You don't have permission to perform this action.");

    // Quality metrics errors
    public static readonly Error InvalidQualityScore = Error.Validation("Trace.InvalidQualityScore", "Quality score must be non-negative.");
    public static readonly Error InvalidTotalBases = Error.Validation("Trace.InvalidTotalBases", "Total bases must be non-negative.");
    public static Error InvalidPercentage(string metric) => Error.Validation("Trace.InvalidPercentage", $"{metric} percentage must be between 0 and 100.");
    public static readonly Error InvalidTrimmedLength = Error.Validation("Trace.InvalidTrimmedLength", "Trimmed length must be between 0 and total bases.");

    // Trim errors
    public static Error InvalidTrimPosition(string position) => Error.Validation("Trace.InvalidTrimPosition", $"Invalid trim position: {position}.");
    public static readonly Error InvalidTrimPositions = Error.Validation("Trace.InvalidTrimPositions", "Trim positions are invalid. End position must be greater than start position.");
    public static readonly Error TrimExceedsSequenceLength = Error.Validation("Trace.TrimExceedsSequenceLength", "Trim positions exceed sequence length.");
    public static readonly Error TrimAlgorithmRequired = Error.Validation("Trace.TrimAlgorithmRequired", "Trim algorithm is required.");
    public static Error TrimAlgorithmTooLong(int max) => Error.Validation("Trace.TrimAlgorithmTooLong", $"Trim algorithm must not exceed {max} characters.");
    public static Error TrimReasonTooLong(int max) => Error.Validation("Trace.TrimReasonTooLong", $"Trim reason must not exceed {max} characters.");
    public static readonly Error TrimmedByRequired = Error.Validation("Trace.TrimmedByRequired", "Trimmed by user is required.");
    public static readonly Error TrimNotFound = Error.NotFound("Trace.TrimNotFound", "Trim not found or already undone.");
    public static readonly Error NoActiveTrims = Error.NotFound("Trace.NoActiveTrims", "There are no active trims to undo.");
    public static readonly Error InvalidTrimBoundaries = Error.Validation("Trace.InvalidTrimBoundaries", "Trim boundaries are outside sequence range.");
    public static readonly Error InvalidTrimEnd = Error.Validation("Trace.InvalidTrimEnd", "Invalid trim end. Must be 'FivePrime' or 'ThreePrime'.");
    public static readonly Error InvalidTrimType = Error.Validation("Trace.InvalidTrimType", "Invalid trim type.");

    // Sequence edit errors
    public static readonly Error EditNotFound = Error.NotFound("Trace.EditNotFound", "Sequence edit was not found.");
    public static readonly Error InvalidEditType = Error.Validation("Trace.InvalidEditType", "Invalid edit type.");
    public static readonly Error InvalidPosition = Error.Validation("Trace.InvalidPosition", "Position must be non-negative and within sequence bounds.");
    public static readonly Error OriginalBaseRequired = Error.Validation("Trace.OriginalBaseRequired", "Original base is required for this edit type.");
    public static readonly Error NewBaseRequired = Error.Validation("Trace.NewBaseRequired", "New base is required for this edit type.");
    public static readonly Error InvalidBase = Error.Validation("Trace.InvalidBase", "Base must be one of A, C, G, T, or N.");
    public static Error ReasonTooLong(int max) => Error.Validation("Trace.ReasonTooLong", $"Edit reason must not exceed {max} characters.");
    public static readonly Error EditAlreadyUndone = Error.Conflict("Trace.EditAlreadyUndone", "This edit has already been undone.");
    public static readonly Error NoActiveEdits = Error.NotFound("Trace.NoActiveEdits", "There are no active edits to undo.");

    // Annotation errors
    public static readonly Error AnnotationNotFound = Error.NotFound("Trace.AnnotationNotFound", "Annotation was not found.");
    public static readonly Error AnnotationLabelRequired = Error.Validation("Trace.AnnotationLabelRequired", "Annotation label is required.");
    public static Error AnnotationLabelTooLong(int max) => Error.Validation("Trace.AnnotationLabelTooLong", $"Annotation label must not exceed {max} characters.");
    public static Error AnnotationDescriptionTooLong(int max) => Error.Validation("Trace.AnnotationDescriptionTooLong", $"Annotation description must not exceed {max} characters.");
    public static readonly Error InvalidAnnotationType = Error.Validation("Trace.InvalidAnnotationType", "Invalid annotation type.");
    public static readonly Error InvalidAnnotationStrand = Error.Validation("Trace.InvalidAnnotationStrand", "Invalid annotation strand.");
    public static readonly Error InvalidAnnotationRange = Error.Validation("Trace.InvalidAnnotationRange", "End position must be greater than or equal to start position.");
    public static readonly Error AnnotationOutOfBounds = Error.Validation("Trace.AnnotationOutOfBounds", "Annotation positions are outside sequence bounds.");
    public static readonly Error InvalidColor = Error.Validation("Trace.InvalidColor", "Invalid color format. Expected hex color (e.g., #FF5733).");

    // Processing errors
    public static readonly Error AlreadyProcessing = Error.Conflict("Trace.AlreadyProcessing", "Trace is already being processed.");
    public static readonly Error NotUploaded = Error.Validation("Trace.NotUploaded", "Trace must be in Uploaded status to start processing.");
    public static readonly Error NotProcessing = Error.Validation("Trace.NotProcessing", "Trace is not currently being processed.");
    public static readonly Error FailureReasonRequired = Error.Validation("Trace.FailureReasonRequired", "Failure reason is required.");
    public static Error FailureReasonTooLong(int max) => Error.Validation("Trace.FailureReasonTooLong", $"Failure reason must not exceed {max} characters.");

    // Datalake / pagination errors
    public static readonly Error InvalidPageNumber = Error.Validation("Trace.InvalidPageNumber", "Page number must be 1 or greater.");
    public static readonly Error InvalidPageSize = Error.Validation("Trace.InvalidPageSize", "Page size must be between 100 and 50,000 bases.");
    public static readonly Error PageOutOfRange = Error.Validation("Trace.PageOutOfRange", "Requested page is beyond the available sequence data.");
    public static readonly Error ChunkedDataNotAvailable = Error.NotFound("Trace.ChunkedDataNotAvailable", "Chunked sequence data is not available for this trace.");
    public static readonly Error AnalysisResultNotFound = Error.NotFound("Trace.AnalysisResultNotFound", "Analysis result was not found.");
    public static Error AnalysisTypeNotFound(string type) => Error.NotFound("Trace.AnalysisTypeNotFound", $"Analysis result of type '{type}' was not found.");
}
