namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// DTO representing a sequence with edits applied.
/// </summary>
public sealed record EditedSequenceDto(
    string TraceId,
    string OriginalSequence,
    string EditedSequence,
    int EditCount,
    IReadOnlyList<SequenceEditDto> AppliedEdits);
