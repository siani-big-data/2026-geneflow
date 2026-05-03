using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces;

/// <summary>
/// File-level errors (file name, content type, storage path, size, checksum,
/// format) and quality-metric errors. Partial of <see cref="TraceErrors"/>.
/// </summary>
public static partial class TraceErrors
{
    /// <summary>File name is required.</summary>
    public static readonly Error FileNameRequired = Error.Validation(
        "Trace.FileNameRequired", "File name is required.");

    /// <summary>File name is too long.</summary>
    public static Error FileNameTooLong(int max) => Error.Validation(
        "Trace.FileNameTooLong", $"File name must not exceed {max} characters.");

    /// <summary>Content type is required.</summary>
    public static readonly Error ContentTypeRequired = Error.Validation(
        "Trace.ContentTypeRequired", "Content type is required.");

    /// <summary>Content type is too long.</summary>
    public static Error ContentTypeTooLong(int max) => Error.Validation(
        "Trace.ContentTypeTooLong", $"Content type must not exceed {max} characters.");

    /// <summary>Storage path is required.</summary>
    public static readonly Error StoragePathRequired = Error.Validation(
        "Trace.StoragePathRequired", "Storage path is required.");

    /// <summary>Storage path is too long.</summary>
    public static Error StoragePathTooLong(int max) => Error.Validation(
        "Trace.StoragePathTooLong", $"Storage path must not exceed {max} characters.");

    /// <summary>Invalid file size.</summary>
    public static readonly Error InvalidFileSize = Error.Validation(
        "Trace.InvalidFileSize", "File size must be greater than zero.");

    /// <summary>File exceeds maximum size.</summary>
    public static Error FileTooLarge(long maxBytes) => Error.Validation(
        "Trace.FileTooLarge", $"File size exceeds the maximum allowed size of {maxBytes / (1024 * 1024)} MB.");

    /// <summary>Checksum is required.</summary>
    public static readonly Error ChecksumRequired = Error.Validation(
        "Trace.ChecksumRequired", "Checksum is required.");

    /// <summary>Checksum is too long.</summary>
    public static Error ChecksumTooLong(int max) => Error.Validation(
        "Trace.ChecksumTooLong", $"Checksum must not exceed {max} characters.");

    /// <summary>Unsupported trace file format.</summary>
    public static readonly Error UnsupportedFormat = Error.Validation(
        "Trace.UnsupportedFormat", "Unsupported trace file format.");

    /// <summary>Invalid quality score.</summary>
    public static readonly Error InvalidQualityScore = Error.Validation(
        "Trace.InvalidQualityScore", "Quality score must be non-negative.");

    /// <summary>Invalid total bases.</summary>
    public static readonly Error InvalidTotalBases = Error.Validation(
        "Trace.InvalidTotalBases", "Total bases must be non-negative.");

    /// <summary>Invalid percentage value.</summary>
    public static Error InvalidPercentage(string metric) => Error.Validation(
        "Trace.InvalidPercentage", $"{metric} percentage must be between 0 and 100.");

    /// <summary>Invalid trimmed length.</summary>
    public static readonly Error InvalidTrimmedLength = Error.Validation(
        "Trace.InvalidTrimmedLength", "Trimmed length must be between 0 and total bases.");
}
