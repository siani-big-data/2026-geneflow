using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.ListAnalysisResults;

/// <summary>
/// Handler for ListAnalysisResultsQuery.
/// Lists all available analysis result types for a trace from datalake storage.
/// </summary>
public sealed class ListAnalysisResultsQueryHandler
    : IQueryHandler<ListAnalysisResultsQuery, Result<IReadOnlyList<string>>>
{
    private readonly ITraceRepository _traceRepository;
    private readonly IDatalakeStorageClient _datalakeClient;

    public ListAnalysisResultsQueryHandler(
        ITraceRepository traceRepository,
        IDatalakeStorageClient datalakeClient)
    {
        _traceRepository = traceRepository;
        _datalakeClient = datalakeClient;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(
        ListAnalysisResultsQuery request,
        CancellationToken cancellationToken)
    {
        // Validate user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<IReadOnlyList<string>>(TraceErrors.InvalidUserId);

        // Validate trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<IReadOnlyList<string>>(TraceErrors.NotFound);

        // Verify trace exists
        var trace = await _traceRepository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<IReadOnlyList<string>>(TraceErrors.NotFound);

        // List analysis results from datalake
        var analysisTypes = await _datalakeClient.ListAnalysisResultsAsync(
            request.TraceId,
            cancellationToken);

        return Result.Success(analysisTypes);
    }
}
