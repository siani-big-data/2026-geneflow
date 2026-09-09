using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Traces.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceCountsByStatus;

/// <summary>
/// Query to get trace counts by status for a study.
/// Requires membership in the study (allows public study access).
/// </summary>
public sealed record GetTraceCountsByStatusQuery(
    string UserId,
    string StudyId) : IQuery<Result<TraceCountsDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
