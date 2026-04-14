using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceCountsByStatus;

/// <summary>
/// Query to get trace counts by status for a study.
/// </summary>
public sealed record GetTraceCountsByStatusQuery(
    string UserId,
    string StudyId) : IQuery<Result<TraceCountsDto>>;
