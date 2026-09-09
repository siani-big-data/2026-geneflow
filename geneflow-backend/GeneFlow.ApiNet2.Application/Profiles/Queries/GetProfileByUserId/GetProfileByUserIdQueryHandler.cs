using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileByUserId;

/// <summary>
/// Handler for getting a profile by user ID.
/// </summary>
public sealed class GetProfileByUserIdQueryHandler
    : IQueryHandler<GetProfileByUserIdQuery, Result<ProfileDto>>
{
    private readonly IProfileRepository _profileRepository;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetProfileByUserIdQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(
        GetProfileByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        // Parse UserId
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        // Get profile
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        return profile.ToDto();
    }
}
