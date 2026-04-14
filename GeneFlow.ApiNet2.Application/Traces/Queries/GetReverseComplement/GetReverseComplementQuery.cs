using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetReverseComplement;

/// <summary>
/// Query to get the reverse complement of a trace sequence.
/// </summary>
public sealed record GetReverseComplementQuery(
    string TraceId,
    bool UseTrimmedSequence = true) : IQuery<Result<ReverseComplementDto>>;
