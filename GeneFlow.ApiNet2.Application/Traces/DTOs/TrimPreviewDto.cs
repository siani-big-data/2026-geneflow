namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// DTO representing a preview of trim operation.
/// </summary>
public sealed record TrimPreviewDto(
    string TraceId,
    int Start5Prime,
    int End5Prime,
    int Start3Prime,
    int End3Prime,
    string Algorithm,
    int OriginalLength,
    int TrimmedLength,
    string PreviewSequence,
    double AverageQualityInTrimmedRegion);
