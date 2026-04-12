using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyPaper;

/// <summary>
/// Handler for RemoveStudyPaperCommand.
/// </summary>
public sealed class RemoveStudyPaperCommandHandler
    : ICommandHandler<RemoveStudyPaperCommand, Result>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public RemoveStudyPaperCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        RemoveStudyPaperCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(StudyErrors.NotFound);

        if (!StudyPaperId.TryParse(request.PaperId, out var paperId) || paperId is null)
            return Result.Failure(StudyErrors.PaperNotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure(StudyErrors.NotFound);

        // Check permission (must be editor or higher)
        var member = study.GetMember(userId);
        if (member is null || !member.Role.CanEditContent)
            return Result.Failure(StudyErrors.InsufficientPermissions);

        // Remove paper
        var result = study.RemovePaper(paperId, userId);
        if (result.IsFailure)
            return result;

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
