using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
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

    public GetStudyMembersQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
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

        return Result.Success(study.Members.ToDtos());
    }
}
