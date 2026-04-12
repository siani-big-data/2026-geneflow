using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ChangeStudyStatus;

/// <summary>
/// Handler for ChangeStudyStatusCommand.
/// </summary>
public sealed class ChangeStudyStatusCommandHandler
    : ICommandHandler<ChangeStudyStatusCommand, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public ChangeStudyStatusCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyDto>> Handle(
        ChangeStudyStatusCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

        // Validate new status
        var newStatus = StudyStatus.FromId(request.NewStatusId);
        if (newStatus is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidStatus);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Change status (ChangeStatus handles permission checks internally)
        var result = study.ChangeStatus(newStatus, userId);
        if (result.IsFailure)
            return Result.Failure<StudyDto>(result.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(study.ToDto());
    }
}
