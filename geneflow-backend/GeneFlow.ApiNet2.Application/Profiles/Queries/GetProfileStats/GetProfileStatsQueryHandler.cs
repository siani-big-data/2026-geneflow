using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileStats;

/// <summary>
/// Handler for getting profile statistics.
/// Queries actual statistics from Studies and Traces repositories,
/// and alignment counters from the usage datamart.
/// </summary>
public sealed class GetProfileStatsQueryHandler
    : IQueryHandler<GetProfileStatsQuery, Result<ProfileStatsDto>>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly ITraceRepository _traceRepository;
    private readonly IUsageStatsRepository _usageStatsRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetProfileStatsQueryHandler(
        IProfileRepository profileRepository,
        IStudyRepository studyRepository,
        ITraceRepository traceRepository,
        IUsageStatsRepository usageStatsRepository)
    {
        _profileRepository = profileRepository;
        _studyRepository = studyRepository;
        _traceRepository = traceRepository;
        _usageStatsRepository = usageStatsRepository;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileStatsDto>> Handle(
        GetProfileStatsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<ProfileStatsDto>(ProfileErrors.NotFound);

        // Get profile to verify it exists and get MemberSince
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result.Failure<ProfileStatsDto>(ProfileErrors.NotFound);

        // Get real-time counts from database
        var totalStudies = await _studyRepository.CountByMemberAsync(userId, cancellationToken);
        var ownedStudies = await _studyRepository.CountByOwnerIdAsync(userId, cancellationToken);

        // Get study IDs where user is member to count traces
        var studies = await _studyRepository.GetByMemberAsync(userId, 1, 1000, cancellationToken: cancellationToken);
        var studyIds = studies.Items.Select(s => s.Id.ToString()).ToList();

        var (processedTraces, pendingTraces) = studyIds.Count > 0
            ? await _traceRepository.CountByUserStudiesAsync(studyIds, cancellationToken)
            : (0, 0);

        var totalTraces = processedTraces + pendingTraces;

        // Alignment counters come from the usage datamart, which the
        // UsageStatsEventProcessor keeps up to date from alignment events.
        var usageStats = await _usageStatsRepository.GetByUserIdAsync(userId, cancellationToken);

        // Get last activity (most recent study modification or trace upload)
        DateTime? lastActivityAt = null;
        if (studies.Items.Any())
        {
            lastActivityAt = studies.Items
                .Select(s => s.ModifiedAt ?? s.CreatedAt)
                .Max();
        }

        return new ProfileStatsDto
        {
            TotalStudies = totalStudies,
            OwnedStudies = ownedStudies,
            TotalTraces = totalTraces,
            TotalAlignments = (int)(usageStats?.AlignmentsTotal ?? 0),
            CompletedAlignments = (int)(usageStats?.AlignmentsCompleted ?? 0),
            LastActivityAt = lastActivityAt,
            MemberSince = profile.CreatedAt
        };
    }
}
