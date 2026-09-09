using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyStats;

/// <summary>
/// Handler for GetStudyStatsQuery.
/// </summary>
public sealed class GetStudyStatsQueryHandler
    : IQueryHandler<GetStudyStatsQuery, Result<StudyStatsDto>>
{
    private readonly IStudyRepository _studyRepository;

    public GetStudyStatsQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<Result<StudyStatsDto>> Handle(
        GetStudyStatsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyStatsDto>(StudyErrors.NotFound);

        // Parse user ID if provided
        UserId? userId = null;
        if (!string.IsNullOrEmpty(request.UserId))
        {
            if (!UserId.TryParse(request.UserId, out userId) || userId is null)
                return Result.Failure<StudyStatsDto>(StudyErrors.InvalidUserId);
        }

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyStatsDto>(StudyErrors.NotFound);

        // Check if starred by current user
        var isStarred = userId is not null &&
                        await _studyRepository.IsStarredByUserAsync(studyId, userId, cancellationToken);

        var stats = new StudyStatsDto
        {
            StudyId = study.Id.ToString(),
            ViewsCount = study.Metrics.ViewsCount,
            StarsCount = study.Metrics.StarsCount,
            MemberCount = study.Members.Count,
            PaperCount = study.Papers.Count(p => !p.IsDeleted),
            IsStarredByCurrentUser = isStarred
        };

        return Result.Success(stats);
    }
}
