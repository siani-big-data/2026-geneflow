using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyStats;

/// <summary>
/// Query to get study statistics.
/// </summary>
public sealed record GetStudyStatsQuery(
    string StudyId,
    string? UserId = null) : IQuery<Result<StudyStatsDto>>;
