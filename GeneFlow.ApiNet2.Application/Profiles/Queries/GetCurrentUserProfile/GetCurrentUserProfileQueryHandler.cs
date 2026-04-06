using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.Application.Profiles.Mappings;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetCurrentUserProfile;

/// <summary>
/// Handler for getting the current user's profile.
/// </summary>
public sealed class GetCurrentUserProfileQueryHandler
    : IQueryHandler<GetCurrentUserProfileQuery, Result<ProfileDto>>
{
    private readonly IProfileRepository _profileRepository;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the handler.
    /// </summary>
    public GetCurrentUserProfileQueryHandler(
        IProfileRepository profileRepository,
        ICurrentUserService currentUserService)
    {
        _profileRepository = profileRepository;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<Result<ProfileDto>> Handle(
        GetCurrentUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        // Check authentication
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        // Get profile
        var profile = await _profileRepository.GetByUserIdAsync(
            _currentUserService.UserId, cancellationToken);

        if (profile is null)
            return Result.Failure<ProfileDto>(ProfileErrors.NotFound);

        return profile.ToDto();
    }
}
