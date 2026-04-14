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
        TrimRegion = dto.TrimRegion?.ToResponse(),
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

    public static TrimRegionResponse ToResponse(this TrimRegionDto dto) => new()
    {
        Start5Prime = dto.Start5Prime,
        End5Prime = dto.End5Prime,
        Start3Prime = dto.Start3Prime,
        End3Prime = dto.End3Prime,
        Algorithm = dto.Algorithm,
        TrimmedBy = dto.TrimmedBy,
        TrimmedAt = dto.TrimmedAt
    };

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
        TrimRegion = dto.TrimRegion.ToResponse(),
        OriginalLength = dto.OriginalLength,
        TrimmedLength = dto.TrimmedLength
    };
}
