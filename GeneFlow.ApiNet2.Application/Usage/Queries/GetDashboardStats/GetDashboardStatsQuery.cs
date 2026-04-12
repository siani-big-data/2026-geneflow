using GeneFlow.ApiNet2.Application.Usage.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Usage.Queries.GetDashboardStats;

/// <summary>
/// Query to get dashboard statistics for the current user.
/// </summary>
public sealed record GetDashboardStatsQuery(string UserId) : IQuery<Result<DashboardStatsDto>>;
