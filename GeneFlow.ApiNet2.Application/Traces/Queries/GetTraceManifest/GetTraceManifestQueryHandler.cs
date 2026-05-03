using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceManifest;

/// <summary>
/// Handler for GetTraceManifestQuery.
/// Retrieves manifest metadata for chunked trace data from datalake storage.
/// </summary>
public sealed class GetTraceManifestQueryHandler
    : IQueryHandler<GetTraceManifestQuery, Result<TraceManifestDto>>
{
    private readonly ITraceRepository _traceRepository;
    private readonly IDatalakeStorageClient _datalakeClient;

    public GetTraceManifestQueryHandler(
        ITraceRepository traceRepository,
        IDatalakeStorageClient datalakeClient)
    {
        _traceRepository = traceRepository;
        _datalakeClient = datalakeClient;
    }

    public async Task<Result<TraceManifestDto>> Handle(
        GetTraceManifestQuery request,
        CancellationToken cancellationToken)
    {
        // Validate user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<TraceManifestDto>(TraceErrors.InvalidUserId);

        // Validate trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<TraceManifestDto>(TraceErrors.NotFound);

        // Verify trace exists
        var trace = await _traceRepository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<TraceManifestDto>(TraceErrors.NotFound);

        // Get manifest from datalake
        var manifest = await _datalakeClient.GetManifestAsync(request.TraceId, cancellationToken);
        if (manifest is null)
            return Result.Failure<TraceManifestDto>(TraceErrors.ChunkedDataNotAvailable);

        return Result.Success(manifest);
    }
}
