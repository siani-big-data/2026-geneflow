using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Commands.CompleteTraceProcessing;

/// <summary>
/// Command to complete trace processing with quality metrics (Worker API).
/// Requires worker API key authentication.
/// </summary>
public sealed record CompleteTraceProcessingCommand(
    string TraceId,
    decimal AverageQualityScore,
    int TotalBases,
    decimal QualityAboveQ20Percentage,
    decimal QualityAboveQ30Percentage,
    int TrimmedLength,
    decimal GcContentPercentage,
    bool HasChromatogramData) : ICommand<Result>, IRequireWorkerApiKey;
