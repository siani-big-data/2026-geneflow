namespace GeneFlow.ApiNet2.Application.Studies.Commands.ExportStudy;

/// <summary>
/// Projection of all data required to assemble a study export ZIP archive.
/// Pure data: no streams; the API endpoint fetches the file bytes from
/// <see cref="GeneFlow.ApiNet2.SharedKernel.Infrastructure.IFileStorageService"/>.
/// </summary>
public sealed record ExportStudyDto(
    string ArchiveFileName,
    StudyMetadataExport Study,
    IReadOnlyList<MemberExportInfo> Members,
    IReadOnlyList<TraceExportEntry> Traces,
    IReadOnlyList<PaperExportEntry> Papers);

/// <summary>Top-level study metadata persisted as <c>study.json</c>.</summary>
public sealed record StudyMetadataExport(
    string Id,
    string Title,
    string Description,
    string ResearchField,
    string Status,
    string? Institution,
    string? PrincipalInvestigator,
    bool IsFeatured,
    StudySettingsExport Settings,
    StudyMetricsExport Metrics,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public sealed record StudySettingsExport(
    bool AllowPublicComments,
    bool AllowDataDownload,
    bool RequireApprovalToJoin);

public sealed record StudyMetricsExport(int Views, int Stars);

/// <summary>Member roster row persisted in <c>members.json</c>.</summary>
public sealed record MemberExportInfo(
    string UserId,
    string Role,
    DateTime JoinedAt,
    string? InvitedBy);

/// <summary>
/// Trace metadata + the storage path the endpoint should fetch from MinIO.
/// <see cref="StoragePath"/> is the relative path passed to
/// <c>IFileStorageService.GetFileAsync</c>; if the bytes come back null
/// the file is treated as missing and skipped in the ZIP.
/// </summary>
public sealed record TraceExportEntry(
    string Id,
    string FileName,
    string ContentType,
    string StoragePath,
    long SizeBytes,
    string Format,
    string Status,
    DateTime? ProcessedAt,
    string? FailureReason,
    TraceQualityMetricsExport? QualityMetrics);

public sealed record TraceQualityMetricsExport(
    decimal AverageQualityScore,
    int TotalBases,
    decimal QualityAboveQ20Percentage,
    decimal QualityAboveQ30Percentage,
    int TrimmedLength,
    decimal GcContentPercentage);

/// <summary>
/// Paper metadata + (when present) the storage path for the PDF.
/// <see cref="StoragePath"/> is null when <see cref="HasFile"/> is false.
/// </summary>
public sealed record PaperExportEntry(
    string Id,
    string Title,
    string? Authors,
    string? Doi,
    string? Journal,
    int? PublicationYear,
    string? Abstract,
    bool HasFile,
    string? FileName,
    long? FileSizeBytes,
    string? StoragePath);
