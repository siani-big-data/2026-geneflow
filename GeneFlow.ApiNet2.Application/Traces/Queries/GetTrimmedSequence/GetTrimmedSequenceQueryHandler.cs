using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTrimmedSequence;

/// <summary>
/// Handler for GetTrimmedSequenceQuery.
/// Returns the sequence with trim region applied.
/// </summary>
public sealed class GetTrimmedSequenceQueryHandler
    : IQueryHandler<GetTrimmedSequenceQuery, Result<TrimmedSequenceDto>>
{
    private readonly ITraceRepository _repository;
    private readonly ITraceAnalysisService _analysisService;

    public GetTrimmedSequenceQueryHandler(
        ITraceRepository repository,
        ITraceAnalysisService analysisService)
    {
        _repository = repository;
        _analysisService = analysisService;
    }

    public async Task<Result<TrimmedSequenceDto>> Handle(
        GetTrimmedSequenceQuery request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<TrimmedSequenceDto>(TraceErrors.NotFound);

        // Get trace
        var trace = await _repository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<TrimmedSequenceDto>(TraceErrors.NotFound);

        // Check if trimmed
        if (trace.TrimRegion is null)
            return Result.Failure<TrimmedSequenceDto>(TraceErrors.NotTrimmed);

        // Get original sequence from analysis file
        var sequenceResult = await _analysisService.GetSequenceAsync(trace, cancellationToken);
        if (sequenceResult.IsFailure)
            return Result.Failure<TrimmedSequenceDto>(sequenceResult.Error);

        var originalSequence = sequenceResult.Value;
        var trimRegion = trace.TrimRegion;

        // Extract trimmed sequence (between End5Prime and Start3Prime)
        var start = trimRegion.End5Prime;
        var length = trimRegion.Start3Prime - trimRegion.End5Prime;

        if (start < 0 || start + length > originalSequence.Length)
            return Result.Failure<TrimmedSequenceDto>(TraceErrors.InvalidTrimBoundaries);

        var trimmedSequence = originalSequence.Substring(start, length);

        return Result.Success(new TrimmedSequenceDto(
            traceId.Value.ToString(),
            originalSequence,
            trimmedSequence,
            trimRegion.ToDto(),
            originalSequence.Length,
            trimmedSequence.Length));
    }
}
