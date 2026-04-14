using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTrimmedSequence;

/// <summary>
/// Query to get the trimmed sequence for a trace.
/// </summary>
public sealed record GetTrimmedSequenceQuery(string TraceId) : IQuery<Result<TrimmedSequenceDto>>;
