using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileStats;

/// <summary>
/// Query to get profile statistics.
/// </summary>
public sealed record GetProfileStatsQuery(string UserId) : IQuery<Result<ProfileStatsDto>>;
