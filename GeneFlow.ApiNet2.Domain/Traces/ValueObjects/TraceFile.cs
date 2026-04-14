using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

/// <summary>
/// Represents trace file metadata and storage information.
/// </summary>
public sealed class TraceFile : ValueObject
{
    public const int MaxFileNameLength = 255;
    public const int MaxContentTypeLength = 100;
    public const int MaxStoragePathLength = 500;
    public const int MaxChecksumLength = 64;
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Original file name.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// MIME content type.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Storage path (MinIO or Supabase).
    /// </summary>
    public string StoragePath { get; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; }

    /// <summary>
    /// SHA-256 checksum for integrity verification.
    /// </summary>
    public string Checksum { get; }

    private TraceFile(
        string fileName,
        string contentType,
        string storagePath,
        long sizeBytes,
        string checksum)
    {
        FileName = fileName;
        ContentType = contentType;
        StoragePath = storagePath;
        SizeBytes = sizeBytes;
        Checksum = checksum;
    }

    public static Result<TraceFile> Create(
        string fileName,
        string contentType,
        string storagePath,
        long sizeBytes,
        string checksum)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure<TraceFile>(TraceErrors.FileNameRequired);

        if (fileName.Length > MaxFileNameLength)
            return Result.Failure<TraceFile>(TraceErrors.FileNameTooLong(MaxFileNameLength));

        if (string.IsNullOrWhiteSpace(contentType))
            return Result.Failure<TraceFile>(TraceErrors.ContentTypeRequired);

        if (contentType.Length > MaxContentTypeLength)
            return Result.Failure<TraceFile>(TraceErrors.ContentTypeTooLong(MaxContentTypeLength));

        if (string.IsNullOrWhiteSpace(storagePath))
            return Result.Failure<TraceFile>(TraceErrors.StoragePathRequired);

        if (storagePath.Length > MaxStoragePathLength)
            return Result.Failure<TraceFile>(TraceErrors.StoragePathTooLong(MaxStoragePathLength));

        if (sizeBytes <= 0)
            return Result.Failure<TraceFile>(TraceErrors.InvalidFileSize);

        if (sizeBytes > MaxFileSizeBytes)
            return Result.Failure<TraceFile>(TraceErrors.FileTooLarge(MaxFileSizeBytes));

        if (string.IsNullOrWhiteSpace(checksum))
            return Result.Failure<TraceFile>(TraceErrors.ChecksumRequired);

        if (checksum.Length > MaxChecksumLength)
            return Result.Failure<TraceFile>(TraceErrors.ChecksumTooLong(MaxChecksumLength));

        return new TraceFile(
            fileName.Trim(),
            contentType.Trim(),
            storagePath.Trim(),
            sizeBytes,
            checksum.Trim());
    }

    /// <summary>
    /// Gets the file extension from the file name.
    /// </summary>
    public string GetExtension()
    {
        var lastDot = FileName.LastIndexOf('.');
        return lastDot >= 0 ? FileName[lastDot..].ToLowerInvariant() : string.Empty;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FileName;
        yield return ContentType;
        yield return StoragePath;
        yield return SizeBytes;
        yield return Checksum;
    }
}
