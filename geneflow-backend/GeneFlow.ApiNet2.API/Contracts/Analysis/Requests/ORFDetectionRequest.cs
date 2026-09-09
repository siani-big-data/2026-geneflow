namespace GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;

/// <summary>
/// Request to detect open reading frames in a trace sequence.
/// </summary>
/// <param name="MinLength">Minimum ORF length in nucleotides. Default: 30.</param>
/// <param name="StartCodons">Start codons to consider. Default: ATG.</param>
/// <param name="StopCodons">Stop codons to consider. Default: TAA,TAG,TGA.</param>
public sealed record ORFDetectionRequest(
    int MinLength = 30,
    string StartCodons = "ATG",
    string StopCodons = "TAA,TAG,TGA");
