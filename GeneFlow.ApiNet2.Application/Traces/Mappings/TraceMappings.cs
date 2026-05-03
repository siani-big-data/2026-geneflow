using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Entities;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Application.Traces.Mappings;

/// <summary>
/// Mapping extensions for Trace domain entities to DTOs.
/// </summary>
public static class TraceMappings
{
    public static TraceDto ToDto(this Trace trace)
    {
        return new TraceDto
        {
            Id = trace.Id.ToString(),
            StudyId = trace.StudyId.ToString(),
            UploadedBy = trace.UploadedBy.ToString(),
            Name = trace.Name.Value,
            Description = trace.Description?.Value,
            FileName = trace.File.FileName,
            ContentType = trace.File.ContentType,
            SizeBytes = trace.File.SizeBytes,
            Format = trace.Format.Name,
            FormatId = trace.Format.Id,
            Status = trace.Status.Name,
            StatusId = trace.Status.Id,
            QualityMetrics = trace.QualityMetrics?.ToDto(),
            Trims = trace.Trims.ToDtos(),
            ActiveTrimCount = trace.ActiveTrimCount,
            HasChromatogramData = trace.HasChromatogramData,
            FailureReason = trace.FailureReason,
            ProcessedAt = trace.ProcessedAt,
            ActiveEditCount = trace.ActiveEditCount,
            AnnotationCount = trace.AnnotationCount,
            CreatedAt = trace.CreatedAt,
            CreatedBy = trace.CreatedBy,
            ModifiedAt = trace.ModifiedAt,
            ModifiedBy = trace.ModifiedBy
        };
    }

    public static TraceSummaryDto ToSummaryDto(this Trace trace)
    {
        return new TraceSummaryDto
        {
            Id = trace.Id.ToString(),
            StudyId = trace.StudyId.ToString(),
            Name = trace.Name.Value,
            Description = trace.Description?.Value,
            FileName = trace.File.FileName,
            SizeBytes = trace.File.SizeBytes,
            Format = trace.Format.Name,
            FormatId = trace.Format.Id,
            Status = trace.Status.Name,
            StatusId = trace.Status.Id,
            AverageQualityScore = trace.QualityMetrics?.AverageQualityScore,
            TotalBases = trace.QualityMetrics?.TotalBases,
            HasChromatogramData = trace.HasChromatogramData,
            ProcessedAt = trace.ProcessedAt,
            CreatedAt = trace.CreatedAt,
            ModifiedAt = trace.ModifiedAt
        };
    }

    public static IReadOnlyList<TraceSummaryDto> ToSummaryDtos(this IEnumerable<Trace> traces)
    {
        return traces.Select(t => t.ToSummaryDto()).ToList();
    }

    public static QualityMetricsDto ToDto(this QualityMetrics metrics)
    {
        return new QualityMetricsDto
        {
            AverageQualityScore = metrics.AverageQualityScore,
            TotalBases = metrics.TotalBases,
            QualityAboveQ20Percentage = metrics.QualityAboveQ20Percentage,
            QualityAboveQ30Percentage = metrics.QualityAboveQ30Percentage,
            TrimmedLength = metrics.TrimmedLength,
            GcContentPercentage = metrics.GcContentPercentage
        };
    }

    public static TraceTrimDto ToDto(this TraceTrim trim)
    {
        return new TraceTrimDto
        {
            Id = trim.Id.ToString(),
            TrimType = trim.TrimType.Name,
            TrimTypeId = trim.TrimType.Id,
            TrimEnd = trim.TrimEnd.Name,
            TrimEndId = trim.TrimEnd.Id,
            StartPosition = trim.StartPosition,
            EndPosition = trim.EndPosition,
            Length = trim.Length,
            Algorithm = trim.Algorithm,
            Reason = trim.Reason,
            AppliedBy = trim.AppliedBy.ToString(),
            AppliedAt = trim.AppliedAt,
            IsActive = trim.IsActive
        };
    }

    public static IReadOnlyList<TraceTrimDto> ToDtos(this IEnumerable<TraceTrim> trims)
    {
        return trims.Select(t => t.ToDto()).ToList();
    }

    public static SequenceEditDto ToDto(this SequenceEdit edit, string? traceId = null)
    {
        return new SequenceEditDto
        {
            Id = edit.Id.ToString(),
            EditType = edit.EditType.Name,
            EditTypeId = edit.EditType.Id,
            Position = edit.Position,
            OriginalBase = edit.OriginalBase,
            NewBase = edit.NewBase,
            Reason = edit.Reason,
            EditedBy = edit.EditedBy.ToString(),
            EditedAt = edit.EditedAt,
            IsActive = edit.IsActive
        };
    }

    public static IReadOnlyList<SequenceEditDto> ToDtos(this IEnumerable<SequenceEdit> edits)
    {
        return edits.Select(e => e.ToDto()).ToList();
    }

    public static AnnotationDto ToDto(this TraceAnnotation annotation, string traceId)
    {
        return new AnnotationDto
        {
            Id = annotation.Id.ToString(),
            TraceId = traceId,
            Type = annotation.Type.Name,
            TypeId = annotation.Type.Id,
            Label = annotation.Label,
            Description = annotation.Description,
            StartPosition = annotation.StartPosition,
            EndPosition = annotation.EndPosition,
            Strand = annotation.Strand.Name,
            StrandId = annotation.Strand.Id,
            Color = annotation.Color,
            IsShared = annotation.IsShared,
            Metadata = annotation.Metadata,
            CreatedAt = annotation.CreatedAt,
            CreatedBy = annotation.CreatedBy,
            ModifiedAt = annotation.ModifiedAt,
            ModifiedBy = annotation.ModifiedBy
        };
    }

    public static IReadOnlyList<AnnotationDto> ToDtos(this IEnumerable<TraceAnnotation> annotations, string traceId)
    {
        return annotations.Select(a => a.ToDto(traceId)).ToList();
    }

    public static TraceCountsDto ToCountsDto(this Dictionary<TraceStatus, int> counts)
    {
        return new TraceCountsDto
        {
            Total = counts.Values.Sum(),
            Uploaded = counts.GetValueOrDefault(TraceStatus.Uploaded, 0),
            Validating = counts.GetValueOrDefault(TraceStatus.Validating, 0),
            Processing = counts.GetValueOrDefault(TraceStatus.Processing, 0),
            Processed = counts.GetValueOrDefault(TraceStatus.Processed, 0),
            Failed = counts.GetValueOrDefault(TraceStatus.Failed, 0),
            Archived = counts.GetValueOrDefault(TraceStatus.Archived, 0)
        };
    }
}
