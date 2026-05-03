namespace GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;

/// <summary>
/// Request to analyze restriction enzyme cut sites in a trace sequence.
/// </summary>
/// <param name="Enzymes">Specific enzymes to analyze (comma-separated). If null, analyzes common enzymes.</param>
public sealed record RestrictionAnalysisRequest(
    string? Enzymes = null);
