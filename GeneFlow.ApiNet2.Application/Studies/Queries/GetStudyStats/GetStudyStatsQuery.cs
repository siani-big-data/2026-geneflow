using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyStats;

/// <summary>
/// Query to get study statistics.
/// Accessible to any member, or to anyone for public studies.
/// </summary>
public sealed record GetStudyStatsQuery(
    string StudyId,
    string? UserId = null) : IQuery<Result<StudyStatsDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => null;
}
