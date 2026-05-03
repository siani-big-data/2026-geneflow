namespace GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;

/// <summary>
/// Request to translate a trace sequence to protein.
/// </summary>
/// <param name="Frame">Reading frame (1, 2, 3, -1, -2, -3). Default: 1.</param>
/// <param name="GeneticCode">Genetic code table to use. Default: 1 (Standard).</param>
public sealed record TranslationRequest(
    int Frame = 1,
    int GeneticCode = 1);
