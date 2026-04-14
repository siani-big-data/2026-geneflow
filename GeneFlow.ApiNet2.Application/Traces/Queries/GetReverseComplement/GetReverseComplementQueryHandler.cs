using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using System.Text;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetReverseComplement;

/// <summary>
/// Handler for GetReverseComplementQuery.
/// Computes the reverse complement of a DNA sequence.
/// </summary>
public sealed class GetReverseComplementQueryHandler
    : IQueryHandler<GetReverseComplementQuery, Result<ReverseComplementDto>>
{
    private readonly ITraceRepository _repository;
    private readonly ITraceAnalysisService _analysisService;

    private static readonly Dictionary<char, char> ComplementMap = new()
    {
        ['A'] = 'T', ['a'] = 't',
        ['T'] = 'A', ['t'] = 'a',
        ['G'] = 'C', ['g'] = 'c',
        ['C'] = 'G', ['c'] = 'g',
        ['N'] = 'N', ['n'] = 'n',
        ['R'] = 'Y', ['r'] = 'y', // Purine (A/G) -> Pyrimidine (T/C)
        ['Y'] = 'R', ['y'] = 'r', // Pyrimidine (T/C) -> Purine (A/G)
        ['M'] = 'K', ['m'] = 'k', // Amino (A/C) -> Keto (T/G)
        ['K'] = 'M', ['k'] = 'm', // Keto (T/G) -> Amino (A/C)
        ['S'] = 'S', ['s'] = 's', // Strong (G/C) -> Strong (G/C)
        ['W'] = 'W', ['w'] = 'w', // Weak (A/T) -> Weak (A/T)
        ['H'] = 'D', ['h'] = 'd', // Not G (A/C/T) -> Not C (A/G/T)
        ['D'] = 'H', ['d'] = 'h', // Not C (A/G/T) -> Not G (A/C/T)
        ['B'] = 'V', ['b'] = 'v', // Not A (C/G/T) -> Not T (A/C/G)
        ['V'] = 'B', ['v'] = 'b', // Not T (A/C/G) -> Not A (C/G/T)
    };

    public GetReverseComplementQueryHandler(
        ITraceRepository repository,
        ITraceAnalysisService analysisService)
    {
        _repository = repository;
        _analysisService = analysisService;
    }

    public async Task<Result<ReverseComplementDto>> Handle(
        GetReverseComplementQuery request,
        CancellationToken cancellationToken)
    {
        // Parse trace ID
        if (!TraceId.TryParse(request.TraceId, out var traceId) || traceId is null)
            return Result.Failure<ReverseComplementDto>(TraceErrors.NotFound);

        // Get trace
        var trace = await _repository.GetByIdAsync(traceId, cancellationToken);
        if (trace is null)
            return Result.Failure<ReverseComplementDto>(TraceErrors.NotFound);

        // Get sequence from analysis file
        var sequenceResult = await _analysisService.GetSequenceAsync(trace, cancellationToken);
        if (sequenceResult.IsFailure)
            return Result.Failure<ReverseComplementDto>(sequenceResult.Error);

        var originalSequence = sequenceResult.Value;

        // Apply trim if requested and available
        if (request.UseTrimmedSequence && trace.TrimRegion is not null)
        {
            var start = trace.TrimRegion.End5Prime;
            var length = trace.TrimRegion.Start3Prime - start;
            originalSequence = originalSequence.Substring(start, length);
        }

        // Compute reverse complement
        var reverseComplement = ComputeReverseComplement(originalSequence);

        return Result.Success(new ReverseComplementDto(
            traceId.Value.ToString(),
            originalSequence,
            reverseComplement,
            reverseComplement.Length));
    }

    private static string ComputeReverseComplement(string sequence)
    {
        var sb = new StringBuilder(sequence.Length);

        // Traverse in reverse order
        for (int i = sequence.Length - 1; i >= 0; i--)
        {
            var baseChar = sequence[i];
            if (ComplementMap.TryGetValue(baseChar, out var complement))
            {
                sb.Append(complement);
            }
            else
            {
                // Unknown base - keep as is
                sb.Append(baseChar);
            }
        }

        return sb.ToString();
    }
}
