namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// DTO representing a trimmed sequence with multiple trim operations applied.
/// </summary>
public sealed record TrimmedSequenceDto(
    string TraceId,
    string OriginalSequence,
    string TrimmedSequence,
    IReadOnlyList<TraceTrimDto> AppliedTrims,
    int OriginalLength,
    int TrimmedLength,
    int TotalBasesTrimmed);
