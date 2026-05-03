namespace GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;

/// <summary>
/// Request to search for a motif pattern in a trace sequence.
/// </summary>
/// <param name="Pattern">The motif pattern to search for. Required.</param>
/// <param name="SearchComplement">Also search the reverse complement strand. Default: false.</param>
/// <param name="UseRegex">Treat pattern as regular expression. Default: false.</param>
public sealed record MotifSearchRequest(
    string Pattern,
    bool SearchComplement = false,
    bool UseRegex = false);
