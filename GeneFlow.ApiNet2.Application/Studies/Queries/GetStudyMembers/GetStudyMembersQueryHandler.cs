using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyMembers;

/// <summary>
/// Handler for GetStudyMembersQuery.
/// </summary>
public sealed class GetStudyMembersQueryHandler
    : IQueryHandler<GetStudyMembersQuery, Result<IReadOnlyList<StudyMemberDto>>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;

    public GetStudyMembersQueryHandler(
        IStudyRepository studyRepository,
        IUserRepository userRepository,
        IProfileRepository profileRepository)
    {
        _studyRepository = studyRepository;
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    public async Task<Result<IReadOnlyList<StudyMemberDto>>> Handle(
        GetStudyMembersQuery request,
        CancellationToken cancellationToken)
    {
        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<IReadOnlyList<StudyMemberDto>>(StudyErrors.NotFound);

        // Parse user ID if provided
        UserId? userId = null;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            if (!UserId.TryParse(request.UserId, out userId) || userId is null)
                return Result.Failure<IReadOnlyList<StudyMemberDto>>(StudyErrors.InvalidUserId);
        }

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<IReadOnlyList<StudyMemberDto>>(StudyErrors.NotFound);

        // Check access - published studies show members publicly
        if (study.Status != StudyStatus.Published)
        {
            // For non-published studies, user must be a member
            if (userId is null || !study.IsMember(userId))
                return Result.Failure<IReadOnlyList<StudyMemberDto>>(StudyErrors.NotFound);
        }

        // Get member user IDs
        var memberUserIds = study.Members.Select(m => m.UserId).ToList();

        // Fetch users and profiles
        var users = await _userRepository.GetByIdsAsync(memberUserIds, cancellationToken);
        var profiles = await _profileRepository.GetByUserIdsAsync(memberUserIds, cancellationToken);

        // Create lookup dictionaries
        var userLookup = users.ToDictionary(u => u.Id.ToString(), u => u);
        var profileLookup = profiles.ToDictionary(p => p.UserId.ToString(), p => p);

        // Map members with user/profile data
        var memberDtos = study.Members.Select(member =>
        {
            var memberUserId = member.UserId.ToString();
            userLookup.TryGetValue(memberUserId, out var user);
            profileLookup.TryGetValue(memberUserId, out var profile);

            var userName = profile is not null
                ? profile.FullName
                : user?.Username.Value;

            return new StudyMemberDto
            {
                UserId = memberUserId,
                Role = member.Role.Name,
                RoleId = member.Role.Id,
                JoinedAt = member.JoinedAt,
                InvitedBy = member.InvitedBy?.ToString(),
                UserName = string.IsNullOrWhiteSpace(userName) ? null : userName,
                UserEmail = user?.Email.Value,
                UserAvatarUrl = profile?.Photo.ThumbnailUrl ?? profile?.Photo.Url
            };
        }).ToList();

        return Result.Success<IReadOnlyList<StudyMemberDto>>(memberDtos);
    }
}
