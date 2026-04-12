using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.IsStudyStarred;

/// <summary>
/// Handler for IsStudyStarredQuery.
/// </summary>
public sealed class IsStudyStarredQueryHandler
    : IQueryHandler<IsStudyStarredQuery, Result<bool>>
{
    private readonly IStudyRepository _studyRepository;

    public IsStudyStarredQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<Result<bool>> Handle(
        IsStudyStarredQuery request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<bool>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<bool>(StudyErrors.InvalidUserId);

        var isStarred = await _studyRepository.IsStarredByUserAsync(studyId, userId, cancellationToken);
        return Result.Success(isStarred);
    }
}
