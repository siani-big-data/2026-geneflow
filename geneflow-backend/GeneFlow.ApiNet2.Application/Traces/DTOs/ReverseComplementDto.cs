namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// DTO representing a reverse complement sequence result.
/// </summary>
public sealed record ReverseComplementDto(
    string TraceId,
    string OriginalSequence,
    string ReverseComplementSequence,
    int Length);
