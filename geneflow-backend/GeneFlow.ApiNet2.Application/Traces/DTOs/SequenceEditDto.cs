namespace GeneFlow.ApiNet2.Application.Traces.DTOs;

/// <summary>
/// Sequence edit data transfer object.
/// </summary>
public sealed record SequenceEditDto
{
    public required string Id { get; init; }
    public required string EditType { get; init; }
    public required int EditTypeId { get; init; }
    public required int Position { get; init; }
    public char? OriginalBase { get; init; }
    public char? NewBase { get; init; }
    public string? Reason { get; init; }
    public required string EditedBy { get; init; }
    public required DateTime EditedAt { get; init; }
    public required bool IsActive { get; init; }
}
