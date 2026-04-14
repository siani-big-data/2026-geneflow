using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetEditedSequence;

/// <summary>
/// Query to get the sequence with edits applied.
/// </summary>
public sealed record GetEditedSequenceQuery(string TraceId) : IQuery<Result<EditedSequenceDto>>;
