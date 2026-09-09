using GeneFlow.ApiNet2.API.Contracts.Traces.Responses;
using GeneFlow.ApiNet2.Application.Traces.DTOs;

namespace GeneFlow.ApiNet2.API.Extensions;

/// <summary>
/// Mapping extensions from Application DTOs to API responses.
/// </summary>
public static class TraceMappingExtensions
{
    public static TraceResponse ToResponse(this TraceDto dto) => new()
    {
        Id = dto.Id,
        StudyId = dto.StudyId,
        UploadedBy = dto.UploadedBy,
        Name = dto.Name,
        Description = dto.Description,
        FileName = dto.FileName,
        ContentType = dto.ContentType,
        SizeBytes = dto.SizeBytes,
        Format = dto.Format,
        FormatId = dto.FormatId,
        Status = dto.Status,
        StatusId = dto.StatusId,
        QualityMetrics = dto.QualityMetrics?.ToResponse(),
        Trims = dto.Trims.ToResponses(),
        ActiveTrimCount = dto.ActiveTrimCount,
        HasChromatogramData = dto.HasChromatogramData,
        FailureReason = dto.FailureReason,
        ProcessedAt = dto.ProcessedAt,
        ActiveEditCount = dto.ActiveEditCount,
        AnnotationCount = dto.AnnotationCount,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        ModifiedAt = dto.ModifiedAt,
        ModifiedBy = dto.ModifiedBy
    };

    public static TraceSummaryResponse ToSummaryResponse(this TraceSummaryDto dto) => new()
    {
        Id = dto.Id,
        StudyId = dto.StudyId,
        Name = dto.Name,
        Description = dto.Description,
        FileName = dto.FileName,
        SizeBytes = dto.SizeBytes,
        Format = dto.Format,
        FormatId = dto.FormatId,
        Status = dto.Status,
        StatusId = dto.StatusId,
        AverageQualityScore = dto.AverageQualityScore,
        TotalBases = dto.TotalBases,
        HasChromatogramData = dto.HasChromatogramData,
        ProcessedAt = dto.ProcessedAt,
        CreatedAt = dto.CreatedAt,
        ModifiedAt = dto.ModifiedAt
    };

    public static IReadOnlyList<TraceSummaryResponse> ToSummaryResponses(
        this IEnumerable<TraceSummaryDto> dtos) =>
        dtos.Select(d => d.ToSummaryResponse()).ToList();

    public static QualityMetricsResponse ToResponse(this QualityMetricsDto dto) => new()
    {
        AverageQualityScore = dto.AverageQualityScore,
        TotalBases = dto.TotalBases,
        QualityAboveQ20Percentage = dto.QualityAboveQ20Percentage,
        QualityAboveQ30Percentage = dto.QualityAboveQ30Percentage,
        TrimmedLength = dto.TrimmedLength,
        GcContentPercentage = dto.GcContentPercentage
    };

    public static TraceTrimResponse ToResponse(this TraceTrimDto dto) => new()
    {
        Id = dto.Id,
        TrimType = dto.TrimType,
        TrimTypeId = dto.TrimTypeId,
        TrimEnd = dto.TrimEnd,
        TrimEndId = dto.TrimEndId,
        StartPosition = dto.StartPosition,
        EndPosition = dto.EndPosition,
        Length = dto.Length,
        Algorithm = dto.Algorithm,
        Reason = dto.Reason,
        AppliedBy = dto.AppliedBy,
        AppliedAt = dto.AppliedAt,
        IsActive = dto.IsActive
    };

    public static IReadOnlyList<TraceTrimResponse> ToResponses(
        this IEnumerable<TraceTrimDto> dtos) =>
        dtos.Select(d => d.ToResponse()).ToList();

    public static SequenceEditResponse ToResponse(this SequenceEditDto dto) => new()
    {
        Id = dto.Id,
        EditType = dto.EditType,
        EditTypeId = dto.EditTypeId,
        Position = dto.Position,
        OriginalBase = dto.OriginalBase,
        NewBase = dto.NewBase,
        Reason = dto.Reason,
        EditedBy = dto.EditedBy,
        EditedAt = dto.EditedAt,
        IsActive = dto.IsActive
    };

    public static IReadOnlyList<SequenceEditResponse> ToResponses(
        this IEnumerable<SequenceEditDto> dtos) =>
        dtos.Select(d => d.ToResponse()).ToList();

    public static AnnotationResponse ToResponse(this AnnotationDto dto) => new()
    {
        Id = dto.Id,
        TraceId = dto.TraceId,
        Type = dto.Type,
        TypeId = dto.TypeId,
        Label = dto.Label,
        Description = dto.Description,
        StartPosition = dto.StartPosition,
        EndPosition = dto.EndPosition,
        Strand = dto.Strand,
        StrandId = dto.StrandId,
        Color = dto.Color,
        IsShared = dto.IsShared,
        Metadata = dto.Metadata,
        CreatedAt = dto.CreatedAt,
        CreatedBy = dto.CreatedBy,
        ModifiedAt = dto.ModifiedAt,
        ModifiedBy = dto.ModifiedBy
    };

    public static IReadOnlyList<AnnotationResponse> ToResponses(
        this IEnumerable<AnnotationDto> dtos) =>
        dtos.Select(d => d.ToResponse()).ToList();

    public static TraceCountsResponse ToResponse(this TraceCountsDto dto) => new()
    {
        Total = dto.Total,
        Uploaded = dto.Uploaded,
        Validating = dto.Validating,
        Processing = dto.Processing,
        Processed = dto.Processed,
        Failed = dto.Failed,
        Archived = dto.Archived
    };

    public static TrimPreviewResponse ToResponse(this TrimPreviewDto dto) => new()
    {
        TraceId = dto.TraceId,
        Start5Prime = dto.Start5Prime,
        End5Prime = dto.End5Prime,
        Start3Prime = dto.Start3Prime,
        End3Prime = dto.End3Prime,
        Algorithm = dto.Algorithm,
        OriginalLength = dto.OriginalLength,
        TrimmedLength = dto.TrimmedLength,
        PreviewSequence = dto.PreviewSequence,
        AverageQualityInTrimmedRegion = dto.AverageQualityInTrimmedRegion
    };

    public static EditedSequenceResponse ToResponse(this EditedSequenceDto dto) => new()
    {
        TraceId = dto.TraceId,
        OriginalSequence = dto.OriginalSequence,
        EditedSequence = dto.EditedSequence,
        EditCount = dto.EditCount,
        AppliedEdits = dto.AppliedEdits.ToResponses()
    };

    public static ReverseComplementResponse ToResponse(this ReverseComplementDto dto) => new()
    {
        TraceId = dto.TraceId,
        OriginalSequence = dto.OriginalSequence,
        ReverseComplementSequence = dto.ReverseComplementSequence,
        Length = dto.Length
    };

    public static TrimmedSequenceResponse ToResponse(this TrimmedSequenceDto dto) => new()
    {
        TraceId = dto.TraceId,
        OriginalSequence = dto.OriginalSequence,
        TrimmedSequence = dto.TrimmedSequence,
        AppliedTrims = dto.AppliedTrims.ToResponses(),
        OriginalLength = dto.OriginalLength,
        TrimmedLength = dto.TrimmedLength,
        TotalBasesTrimmed = dto.TotalBasesTrimmed
    };

    public static SequencePageResponse ToResponse(this SequencePageDto dto) => new()
    {
        TraceId = dto.TraceId,
        Page = dto.Page,
        PageSize = dto.PageSize,
        TotalBases = dto.TotalBases,
        TotalPages = dto.TotalPages,
        Bases = dto.Bases,
        QualityScores = dto.QualityScores,
        Chromatogram = dto.Chromatogram?.ToResponse()
    };

    public static ChromatogramDataResponse ToResponse(this ChromatogramChunkDto dto) => new()
    {
        AChannel = dto.A,
        TChannel = dto.T,
        GChannel = dto.G,
        CChannel = dto.C,
        PeakPositions = dto.PeakPositions
    };

    public static TraceManifestResponse ToResponse(this TraceManifestDto dto) => new()
    {
        TraceId = dto.TraceId,
        OriginalFilename = dto.OriginalFilename,
        Format = dto.Format,
        TotalBases = dto.TotalBases,
        ChunkSize = dto.ChunkSize,
        ChunkCount = dto.ChunkCount,
        HasChromatogram = dto.HasChromatogram,
        HasQualityScores = dto.HasQualityScores,
        CreatedAt = dto.CreatedAt,
        Chunks = dto.Chunks.Select(c => c.ToResponse()).ToList()
    };

    public static ChunkMetadataResponse ToResponse(this ChunkMetadataDto dto) => new()
    {
        Index = dto.Index,
        StartPosition = dto.StartPosition,
        EndPosition = dto.EndPosition,
        BaseCount = dto.BaseCount
    };

    public static AnalysisResultsListResponse ToResponse(string traceId, IReadOnlyList<string> types) => new()
    {
        TraceId = traceId,
        AvailableTypes = types
    };
}
