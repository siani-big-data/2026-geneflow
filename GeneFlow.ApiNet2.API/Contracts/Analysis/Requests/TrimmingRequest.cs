namespace GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;

/// <summary>
/// Request to trim a trace sequence.
/// </summary>
/// <param name="Algorithm">Trimming algorithm to use (modified_mott, window, adaptive). Default: modified_mott.</param>
/// <param name="QualityThreshold">Minimum quality threshold. Default: 20.</param>
/// <param name="WindowSize">Window size for window-based trimming. Default: 10.</param>
public sealed record TrimmingRequest(
    string Algorithm = "modified_mott",
    int QualityThreshold = 20,
    int WindowSize = 10);
