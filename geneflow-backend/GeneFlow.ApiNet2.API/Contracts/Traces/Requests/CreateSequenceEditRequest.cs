namespace GeneFlow.ApiNet2.API.Contracts.Traces.Requests;

/// <summary>
/// Request to create a sequence edit.
/// </summary>
public sealed record CreateSequenceEditRequest(
    int EditType,
    int Position,
    char? OriginalBase,
    char? NewBase,
    string? Reason);
