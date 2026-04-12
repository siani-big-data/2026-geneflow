using GeneFlow.ApiNet2.Application.Usage.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Usage.Queries.GetDashboardStats;

/// <summary>
/// Handler for GetDashboardStatsQuery.
/// </summary>
public sealed class GetDashboardStatsQueryHandler
    : IQueryHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    private readonly IUsageStatsRepository _usageRepository;

    public GetDashboardStatsQueryHandler(IUsageStatsRepository usageRepository)
    {
        _usageRepository = usageRepository;
    }

    public async Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<DashboardStatsDto>(UsageErrors.UserNotFound);

        // Get usage stats
        var stats = await _usageRepository.GetByUserIdAsync(userId, cancellationToken);

        // Return stats (or zeros if not exists)
        return new DashboardStatsDto(
            ActiveStudies: stats?.StudiesTotal ?? 0,
            ProcessedTraces: stats?.TracesTotal ?? 0,
            PendingTraces: stats?.TracesPending ?? 0,
            TeamActivity: 0, // TODO: Track active collaborators
            AlignmentsCompleted: (int)(stats?.AlignmentsCompleted ?? 0));
    }
}
