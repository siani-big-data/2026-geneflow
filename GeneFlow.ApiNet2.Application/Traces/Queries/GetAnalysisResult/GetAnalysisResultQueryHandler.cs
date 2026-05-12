using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetAnalysisResult;

/// <summary>
/// Handler for GetAnalysisResultQuery.
/// Reads through the Redis-backed analysis result store and falls back to datalake
/// storage if the cache is cold; on a datalake hit the cache is populated.
/// </summary>
public sealed class GetAnalysisResultQueryHandler
    : IQueryHandler<GetAnalysisResultQuery, Result<string>>
{
    private readonly ITraceRepository _traceRepository;
    private readonly IDatalakeStorageClient _datalakeClient;
    private readonly IAnalysisResultStore _resultStore;

    public GetAnalysisResultQueryHandler(
        ITraceRepository traceRepository,
        IDatalakeStorageClient datalakeClient,
        IAnalysisResultStore resultStore)
    {
        _traceRepository = traceRepository;
        _datalakeClient = datalakeClient;
        _resultStore = resultStore;
    }

    public async Task<Result<string>> Handle(
        GetAnalysisResultQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<string>(TraceErrors.InvalidUserId);

        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<string>(TraceErrors.NotFound);

        if (string.IsNullOrWhiteSpace(request.AnalysisType))
            return Result.Failure<string>(TraceErrors.AnalysisResultNotFound);

        var trace = await _traceRepository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<string>(TraceErrors.NotFound);

        var cached = await _resultStore.GetAsync(
            request.TraceId, request.AnalysisType, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var analysisJson = await _datalakeClient.GetAnalysisResultJsonAsync(
            request.TraceId,
            request.AnalysisType,
            cancellationToken);

        if (analysisJson is null)
            return Result.Failure<string>(TraceErrors.AnalysisTypeNotFound(request.AnalysisType));

        await _resultStore.SaveAsync(
            request.TraceId, request.AnalysisType, analysisJson, cancellationToken);

        return Result.Success(analysisJson);
    }
}
