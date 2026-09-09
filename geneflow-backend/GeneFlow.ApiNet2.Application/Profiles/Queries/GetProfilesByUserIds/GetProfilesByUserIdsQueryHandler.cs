using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfilesByUserIds;

/// <summary>
/// Handler for getting multiple profiles by user IDs.
/// </summary>
public sealed class GetProfilesByUserIdsQueryHandler
    : IQueryHandler<GetProfilesByUserIdsQuery, Result<IReadOnlyList<ProfileSummaryDto>>>
{
    private readonly IProfileRepository _profileRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetProfilesByUserIdsQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ProfileSummaryDto>>> Handle(
        GetProfilesByUserIdsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserIds.Count == 0)
            return Result.Success<IReadOnlyList<ProfileSummaryDto>>(Array.Empty<ProfileSummaryDto>());

        // Parse UserIds
        var userIds = new List<UserId>();
        foreach (var userIdStr in request.UserIds)
        {
            if (UserId.TryParse(userIdStr, out var userId) && userId is not null)
            {
                userIds.Add(userId);
            }
        }

        if (userIds.Count == 0)
            return Result.Success<IReadOnlyList<ProfileSummaryDto>>(Array.Empty<ProfileSummaryDto>());

        // Get profiles
        var profiles = await _profileRepository.GetByUserIdsAsync(userIds, cancellationToken);

        return Result.Success<IReadOnlyList<ProfileSummaryDto>>(profiles.ToSummaryDtos());
    }
}
