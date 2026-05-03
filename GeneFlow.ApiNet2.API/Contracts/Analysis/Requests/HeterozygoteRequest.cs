namespace GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;

/// <summary>
/// Request to detect heterozygous positions in a trace.
/// </summary>
/// <param name="SecondaryPeakThreshold">Minimum ratio for secondary peak detection. Default: 0.3.</param>
/// <param name="MinQuality">Minimum quality score at heterozygote position. Default: 20.</param>
public sealed record HeterozygoteRequest(
    double SecondaryPeakThreshold = 0.3,
    int MinQuality = 20);
