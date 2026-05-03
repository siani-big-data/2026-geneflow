using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetAnalysisResult;

/// <summary>
/// Handler for GetAnalysisResultQuery.
/// Retrieves a specific analysis result JSON from datalake storage.
/// </summary>
public sealed class GetAnalysisResultQueryHandler
    : IQueryHandler<GetAnalysisResultQuery, Result<string>>
{
    private readonly ITraceRepository _traceRepository;
    private readonly IDatalakeStorageClient _datalakeClient;

    public GetAnalysisResultQueryHandler(
        ITraceRepository traceRepository,
        IDatalakeStorageClient datalakeClient)
    {
        _traceRepository = traceRepository;
        _datalakeClient = datalakeClient;
    }

    public async Task<Result<string>> Handle(
        GetAnalysisResultQuery request,
        CancellationToken cancellationToken)
    {
        // Validate user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<string>(TraceErrors.InvalidUserId);

        // Validate trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<string>(TraceErrors.NotFound);

        // Validate analysis type
        if (string.IsNullOrWhiteSpace(request.AnalysisType))
            return Result.Failure<string>(TraceErrors.AnalysisResultNotFound);

        // Verify trace exists
        var trace = await _traceRepository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<string>(TraceErrors.NotFound);

        // Get analysis result from datalake
        var analysisJson = await _datalakeClient.GetAnalysisResultJsonAsync(
            request.TraceId,
            request.AnalysisType,
            cancellationToken);

        if (analysisJson is null)
            return Result.Failure<string>(TraceErrors.AnalysisTypeNotFound(request.AnalysisType));

        return Result.Success(analysisJson);
    }
}
