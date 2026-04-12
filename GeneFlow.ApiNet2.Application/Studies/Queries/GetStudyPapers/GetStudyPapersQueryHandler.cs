using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyPapers;

/// <summary>
/// Handler for GetStudyPapersQuery.
/// </summary>
public sealed class GetStudyPapersQueryHandler
    : IQueryHandler<GetStudyPapersQuery, Result<IReadOnlyList<StudyPaperDto>>>
{
    private readonly IStudyRepository _studyRepository;

    public GetStudyPapersQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<Result<IReadOnlyList<StudyPaperDto>>> Handle(
        GetStudyPapersQuery request,
        CancellationToken cancellationToken)
    {
        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<IReadOnlyList<StudyPaperDto>>(StudyErrors.NotFound);

        // Parse user ID if provided
        UserId? userId = null;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            if (!UserId.TryParse(request.UserId, out userId) || userId is null)
                return Result.Failure<IReadOnlyList<StudyPaperDto>>(StudyErrors.InvalidUserId);
        }

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<IReadOnlyList<StudyPaperDto>>(StudyErrors.NotFound);

        // Check access - published studies show papers publicly
        if (study.Status != StudyStatus.Published)
        {
            // For non-published studies, user must be a member
            if (userId is null || !study.IsMember(userId))
                return Result.Failure<IReadOnlyList<StudyPaperDto>>(StudyErrors.NotFound);
        }

        // Return only non-deleted papers
        var papers = study.Papers.Where(p => !p.IsDeleted).ToDtos();
        return Result.Success(papers);
    }
}
