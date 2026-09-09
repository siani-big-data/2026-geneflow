using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// Errors related to trim operations, sequence edits and annotations on a trace.
/// Partial of <see cref="TraceErrors"/>.
/// </summary>
public static partial class TraceErrors
{
    /// <summary>Invalid trim position.</summary>
    public static Error InvalidTrimPosition(string position) => Error.Validation(
        "Trace.InvalidTrimPosition", $"Invalid trim position: {position}.");

    /// <summary>Invalid trim positions.</summary>
    public static readonly Error InvalidTrimPositions = Error.Validation(
        "Trace.InvalidTrimPositions", "Trim positions are invalid. End position must be greater than start position.");

    /// <summary>Trim exceeds sequence length.</summary>
    public static readonly Error TrimExceedsSequenceLength = Error.Validation(
        "Trace.TrimExceedsSequenceLength", "Trim positions exceed sequence length.");

    /// <summary>Trim algorithm is required.</summary>
    public static readonly Error TrimAlgorithmRequired = Error.Validation(
        "Trace.TrimAlgorithmRequired", "Trim algorithm is required.");

    /// <summary>Trim algorithm is too long.</summary>
    public static Error TrimAlgorithmTooLong(int max) => Error.Validation(
        "Trace.TrimAlgorithmTooLong", $"Trim algorithm must not exceed {max} characters.");

    /// <summary>Trim reason is too long.</summary>
    public static Error TrimReasonTooLong(int max) => Error.Validation(
        "Trace.TrimReasonTooLong", $"Trim reason must not exceed {max} characters.");

    /// <summary>TrimmedBy user is required.</summary>
    public static readonly Error TrimmedByRequired = Error.Validation(
        "Trace.TrimmedByRequired", "Trimmed by user is required.");

    /// <summary>Trim was not found or already undone.</summary>
    public static readonly Error TrimNotFound = Error.NotFound(
        "Trace.TrimNotFound", "Trim not found or already undone.");

    /// <summary>No active trims to undo.</summary>
    public static readonly Error NoActiveTrims = Error.NotFound(
        "Trace.NoActiveTrims", "There are no active trims to undo.");

    /// <summary>Trim boundaries are out of range.</summary>
    public static readonly Error InvalidTrimBoundaries = Error.Validation(
        "Trace.InvalidTrimBoundaries", "Trim boundaries are outside sequence range.");

    /// <summary>Invalid trim end.</summary>
    public static readonly Error InvalidTrimEnd = Error.Validation(
        "Trace.InvalidTrimEnd", "Invalid trim end. Must be 'FivePrime' or 'ThreePrime'.");

    /// <summary>Invalid trim type.</summary>
    public static readonly Error InvalidTrimType = Error.Validation(
        "Trace.InvalidTrimType", "Invalid trim type.");

    /// <summary>Sequence edit was not found.</summary>
    public static readonly Error EditNotFound = Error.NotFound(
        "Trace.EditNotFound", "Sequence edit was not found.");

    /// <summary>Invalid edit type.</summary>
    public static readonly Error InvalidEditType = Error.Validation(
        "Trace.InvalidEditType", "Invalid edit type.");

    /// <summary>Invalid sequence position.</summary>
    public static readonly Error InvalidPosition = Error.Validation(
        "Trace.InvalidPosition", "Position must be non-negative and within sequence bounds.");

    /// <summary>Original base is required for this edit type.</summary>
    public static readonly Error OriginalBaseRequired = Error.Validation(
        "Trace.OriginalBaseRequired", "Original base is required for this edit type.");

    /// <summary>New base is required for this edit type.</summary>
    public static readonly Error NewBaseRequired = Error.Validation(
        "Trace.NewBaseRequired", "New base is required for this edit type.");

    /// <summary>Invalid base.</summary>
    public static readonly Error InvalidBase = Error.Validation(
        "Trace.InvalidBase", "Base must be one of A, C, G, T, or N.");

    /// <summary>Edit reason is too long.</summary>
    public static Error ReasonTooLong(int max) => Error.Validation(
        "Trace.ReasonTooLong", $"Edit reason must not exceed {max} characters.");

    /// <summary>Edit has already been undone.</summary>
    public static readonly Error EditAlreadyUndone = Error.Conflict(
        "Trace.EditAlreadyUndone", "This edit has already been undone.");

    /// <summary>No active edits to undo.</summary>
    public static readonly Error NoActiveEdits = Error.NotFound(
        "Trace.NoActiveEdits", "There are no active edits to undo.");

    /// <summary>Annotation was not found.</summary>
    public static readonly Error AnnotationNotFound = Error.NotFound(
        "Trace.AnnotationNotFound", "Annotation was not found.");

    /// <summary>Annotation label is required.</summary>
    public static readonly Error AnnotationLabelRequired = Error.Validation(
        "Trace.AnnotationLabelRequired", "Annotation label is required.");

    /// <summary>Annotation label is too long.</summary>
    public static Error AnnotationLabelTooLong(int max) => Error.Validation(
        "Trace.AnnotationLabelTooLong", $"Annotation label must not exceed {max} characters.");

    /// <summary>Annotation description is too long.</summary>
    public static Error AnnotationDescriptionTooLong(int max) => Error.Validation(
        "Trace.AnnotationDescriptionTooLong", $"Annotation description must not exceed {max} characters.");

    /// <summary>Invalid annotation type.</summary>
    public static readonly Error InvalidAnnotationType = Error.Validation(
        "Trace.InvalidAnnotationType", "Invalid annotation type.");

    /// <summary>Invalid annotation strand.</summary>
    public static readonly Error InvalidAnnotationStrand = Error.Validation(
        "Trace.InvalidAnnotationStrand", "Invalid annotation strand.");

    /// <summary>Invalid annotation range.</summary>
    public static readonly Error InvalidAnnotationRange = Error.Validation(
        "Trace.InvalidAnnotationRange", "End position must be greater than or equal to start position.");

    /// <summary>Annotation is out of bounds.</summary>
    public static readonly Error AnnotationOutOfBounds = Error.Validation(
        "Trace.AnnotationOutOfBounds", "Annotation positions are outside sequence bounds.");

    /// <summary>Invalid color format.</summary>
    public static readonly Error InvalidColor = Error.Validation(
        "Trace.InvalidColor", "Invalid color format. Expected hex color (e.g., #FF5733).");
}
