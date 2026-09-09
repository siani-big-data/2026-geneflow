namespace GeneFlow.ApiNet2.API.Contracts.Traces.Requests;

/// <summary>
/// Request to complete trace processing (Worker API).
/// </summary>
public sealed record CompleteProcessingRequest(
    decimal AverageQualityScore,
    int TotalBases,
    decimal QualityAboveQ20Percentage,
    decimal QualityAboveQ30Percentage,
    int TrimmedLength,
    decimal GcContentPercentage,
    bool HasChromatogramData);
