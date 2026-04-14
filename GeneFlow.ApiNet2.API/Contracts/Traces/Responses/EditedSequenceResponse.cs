namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Edited sequence response.
/// </summary>
public sealed class EditedSequenceResponse
{
    public string TraceId { get; init; } = null!;
    public string OriginalSequence { get; init; } = null!;
    public string EditedSequence { get; init; } = null!;
    public int EditCount { get; init; }
    public IReadOnlyList<SequenceEditResponse> AppliedEdits { get; init; } = [];
}
