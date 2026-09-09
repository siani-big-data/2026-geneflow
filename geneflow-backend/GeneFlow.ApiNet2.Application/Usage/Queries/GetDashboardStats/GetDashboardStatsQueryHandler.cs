using GeneFlow.ApiNet2.Application.Usage.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Usage.Queries.GetDashboardStats;

/// <summary>
/// Handler for GetDashboardStatsQuery.
/// Calculates real-time statistics from database.
/// </summary>
public sealed class GetDashboardStatsQueryHandler
    : IQueryHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly ITraceRepository _traceRepository;

    public GetDashboardStatsQueryHandler(
        IStudyRepository studyRepository,
        ITraceRepository traceRepository)
    {
        _studyRepository = studyRepository;
        _traceRepository = traceRepository;
    }

    public async Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<DashboardStatsDto>(UsageErrors.UserNotFound);

        // Get real-time counts from database
        var activeStudies = await _studyRepository.CountByMemberAsync(userId, cancellationToken);
        var teamMembers = await _studyRepository.CountMembersInUserStudiesAsync(userId, cancellationToken);

        // Get study IDs where user is member to count traces
        var studies = await _studyRepository.GetByMemberAsync(userId, 1, 1000, cancellationToken: cancellationToken);
        var studyIds = studies.Items.Select(s => s.Id.ToString()).ToList();

        var (processedTraces, pendingTraces) = studyIds.Count > 0
            ? await _traceRepository.CountByUserStudiesAsync(studyIds, cancellationToken)
            : (0, 0);

        return new DashboardStatsDto(
            ActiveStudies: activeStudies,
            ProcessedTraces: processedTraces,
            PendingTraces: pendingTraces,
            TeamActivity: teamMembers,
            AlignmentsCompleted: 0);
    }
}
