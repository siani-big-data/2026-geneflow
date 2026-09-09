using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Orchestration;

/// <summary>
/// Loads sequence + quality data for a trace from MinIO so the orchestrator can
/// build <see cref="SharedKernel.Infrastructure.AnalysisJob"/> payloads. Wraps
/// <see cref="ITraceAnalysisService"/> and the trace repository to keep the
/// orchestrator decoupled from persistence concerns.
/// </summary>
public sealed class PipelineTraceDataResolver
{
    private readonly ITraceRepository _traceRepository;
    private readonly ITraceAnalysisService _analysisService;

    public PipelineTraceDataResolver(
        ITraceRepository traceRepository,
        ITraceAnalysisService analysisService)
    {
        _traceRepository = traceRepository;
        _analysisService = analysisService;
    }

    /// <summary>
    /// Resolves the trace's parsed sequence and quality scores. Returns a
    /// failure result if the trace cannot be loaded or its parsed data is
    /// missing.
    /// </summary>
    public async Task<Result<(string Sequence, int[] Quality)>> ResolveAsync(
        string rawTraceId,
        CancellationToken cancellationToken)
    {
        if (!TraceId.TryParse(rawTraceId, out var traceId) || traceId is null)
        {
            return Result.Failure<(string, int[])>(
                Error.Validation("Pipeline.InvalidTraceId", $"Invalid trace id '{rawTraceId}'"));
        }

        var trace = await _traceRepository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
        {
            return Result.Failure<(string, int[])>(
                Error.NotFound("Pipeline.TraceNotFound", $"Trace '{rawTraceId}' not found"));
        }

        return await _analysisService.GetAnalysisDataAsync(trace, cancellationToken);
    }
}
