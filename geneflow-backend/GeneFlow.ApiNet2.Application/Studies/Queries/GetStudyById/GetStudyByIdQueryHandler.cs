using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyById;

/// <summary>
/// Handler for GetStudyByIdQuery.
/// </summary>
public sealed class GetStudyByIdQueryHandler
    : IQueryHandler<GetStudyByIdQuery, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;

    public GetStudyByIdQueryHandler(
        IStudyRepository studyRepository,
        IUserRepository userRepository,
        IProfileRepository profileRepository)
    {
        _studyRepository = studyRepository;
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    public async Task<Result<StudyDto>> Handle(
        GetStudyByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Parse user ID if provided
        UserId? userId = null;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            if (!UserId.TryParse(request.UserId, out userId) || userId is null)
                return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);
        }

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Check access permissions
        // Published studies are public, non-published require membership
        if (study.Status != StudyStatus.Published)
        {
            if (userId is null || !study.IsMember(userId))
                return Result.Failure<StudyDto>(StudyErrors.NotFound);
        }

        // Get member user IDs
        var memberUserIds = study.Members.Select(m => m.UserId).ToList();

        // Fetch users and profiles for members
        var users = await _userRepository.GetByIdsAsync(memberUserIds, cancellationToken);
        var profiles = await _profileRepository.GetByUserIdsAsync(memberUserIds, cancellationToken);

        // Create lookup dictionaries
        var userLookup = users.ToDictionary(u => u.Id.ToString(), u => u);
        var profileLookup = profiles.ToDictionary(p => p.UserId.ToString(), p => p);

        return Result.Success(study.ToDto(userLookup, profileLookup, userId));
    }
}
