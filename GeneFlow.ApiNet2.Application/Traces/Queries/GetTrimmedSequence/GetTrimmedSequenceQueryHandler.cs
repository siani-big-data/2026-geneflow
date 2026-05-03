using System.Text;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Mappings;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Entities;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTrimmedSequence;

/// <summary>
/// Handler for GetTrimmedSequenceQuery.
/// Returns the sequence with all active trim operations applied.
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

        // Get trace with trims
        var trace = await _repository.GetByIdWithTrimsAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<TrimmedSequenceDto>(TraceErrors.NotFound);

        // Get active trims
        var activeTrims = trace.GetActiveTrims();
        if (activeTrims.Count == 0)
            return Result.Failure<TrimmedSequenceDto>(TraceErrors.NoActiveTrims);

        // Get original sequence from analysis file
        var sequenceResult = await _analysisService.GetSequenceAsync(trace, cancellationToken);
        if (sequenceResult.IsFailure)
            return Result.Failure<TrimmedSequenceDto>(sequenceResult.Error);

        var originalSequence = sequenceResult.Value;

        // Apply all active trims to get the trimmed sequence
        var trimmedSequence = ApplyTrims(originalSequence, activeTrims);
        var totalBasesTrimmed = originalSequence.Length - trimmedSequence.Length;

        return Result.Success(new TrimmedSequenceDto(
            traceId.Value.ToString(),
            originalSequence,
            trimmedSequence,
            activeTrims.ToDtos(),
            originalSequence.Length,
            trimmedSequence.Length,
            totalBasesTrimmed));
    }

    /// <summary>
    /// Applies all trim operations to a sequence.
    /// Trims are applied from the end of the sequence towards the beginning
    /// to maintain correct positions.
    /// </summary>
    private static string ApplyTrims(string sequence, IReadOnlyList<TraceTrim> trims)
    {
        if (trims.Count == 0) return sequence;

        // Sort trims by position descending to apply from end to start
        // This ensures that earlier trim positions remain valid after later trims are applied
        var sortedTrims = trims
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.StartPosition)
            .ToList();

        var sb = new StringBuilder(sequence);

        foreach (var trim in sortedTrims)
        {
            var start = Math.Max(0, trim.StartPosition);
            var length = Math.Min(sb.Length - start, trim.EndPosition - trim.StartPosition);

            if (start < sb.Length && length > 0)
            {
                sb.Remove(start, length);
            }
        }

        return sb.ToString();
    }
}
