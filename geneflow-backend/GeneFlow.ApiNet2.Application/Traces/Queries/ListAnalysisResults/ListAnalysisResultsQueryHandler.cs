using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.ListAnalysisResults;

/// <summary>
/// Handler for ListAnalysisResultsQuery.
/// Reads the union of analysis types stored in the Redis cache and the datalake,
/// so newly produced results are visible even before the cache has been written to.
/// </summary>
public sealed class ListAnalysisResultsQueryHandler
    : IQueryHandler<ListAnalysisResultsQuery, Result<IReadOnlyList<string>>>
{
    private readonly ITraceRepository _traceRepository;
    private readonly IDatalakeStorageClient _datalakeClient;
    private readonly IAnalysisResultStore _resultStore;

    public ListAnalysisResultsQueryHandler(
        ITraceRepository traceRepository,
        IDatalakeStorageClient datalakeClient,
        IAnalysisResultStore resultStore)
    {
        _traceRepository = traceRepository;
        _datalakeClient = datalakeClient;
        _resultStore = resultStore;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(
        ListAnalysisResultsQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<IReadOnlyList<string>>(TraceErrors.InvalidUserId);

        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<IReadOnlyList<string>>(TraceErrors.NotFound);

        var trace = await _traceRepository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<IReadOnlyList<string>>(TraceErrors.NotFound);

        var cached = await _resultStore.ListAsync(request.TraceId, cancellationToken);
        var datalake = await _datalakeClient.ListAnalysisResultsAsync(
            request.TraceId, cancellationToken);

        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var meta in cached)
            merged.Add(meta.AnalysisType);
        foreach (var type in datalake)
            merged.Add(type);

        return Result.Success<IReadOnlyList<string>>(merged.ToList());
    }
}
