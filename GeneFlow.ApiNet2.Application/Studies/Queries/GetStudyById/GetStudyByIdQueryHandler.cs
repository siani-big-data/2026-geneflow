using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
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

    public GetStudyByIdQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
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
        // Published studies are public
        if (study.Status == StudyStatus.Published)
            return Result.Success(study.ToDto());

        // For non-published studies, user must be a member
        if (userId is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        if (!study.IsMember(userId))
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        return Result.Success(study.ToDto());
    }
}
