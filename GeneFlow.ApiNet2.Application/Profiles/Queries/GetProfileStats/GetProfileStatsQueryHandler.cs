using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileStats;

/// <summary>
/// Handler for getting profile statistics.
/// Note: Returns placeholder values until Studies/Traces modules are migrated.
/// </summary>
public sealed class GetProfileStatsQueryHandler
    : IQueryHandler<GetProfileStatsQuery, Result<ProfileStatsDto>>
{
    private readonly IProfileRepository _profileRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetProfileStatsQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
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

        // TODO: When Studies/Traces modules are migrated, query actual statistics
        // For now, return placeholder values
        return new ProfileStatsDto
        {
            TotalStudies = 0,
            OwnedStudies = 0,
            TotalTraces = 0,
            TotalAlignments = 0,
            CompletedAlignments = 0,
            LastActivityAt = null,
            MemberSince = profile.CreatedAt
        };
    }
}
