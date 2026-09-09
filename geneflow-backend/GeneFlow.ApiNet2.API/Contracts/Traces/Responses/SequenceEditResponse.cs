namespace GeneFlow.ApiNet2.API.Contracts.Traces.Responses;

/// <summary>
/// Sequence edit response.
/// </summary>
public sealed class SequenceEditResponse
{
    public string Id { get; init; } = null!;
    public string EditType { get; init; } = null!;
    public int EditTypeId { get; init; }
    public int Position { get; init; }
    public char? OriginalBase { get; init; }
    public char? NewBase { get; init; }
    public string? Reason { get; init; }
    public string EditedBy { get; init; } = null!;
    public DateTime EditedAt { get; init; }
    public bool IsActive { get; init; }
}
