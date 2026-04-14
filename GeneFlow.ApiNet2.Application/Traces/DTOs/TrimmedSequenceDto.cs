namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// DTO representing a trimmed sequence.
/// </summary>
public sealed record TrimmedSequenceDto(
    string TraceId,
    string OriginalSequence,
    string TrimmedSequence,
    TrimRegionDto TrimRegion,
    int OriginalLength,
    int TrimmedLength);
