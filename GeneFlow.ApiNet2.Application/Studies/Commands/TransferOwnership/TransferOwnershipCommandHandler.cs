using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.TransferOwnership;

/// <summary>
/// Handler for TransferOwnershipCommand.
/// </summary>
public sealed class TransferOwnershipCommandHandler
    : ICommandHandler<TransferOwnershipCommand, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public TransferOwnershipCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyDto>> Handle(
        TransferOwnershipCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.CurrentOwnerId, out var currentOwnerId) || currentOwnerId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

        if (!UserId.TryParse(request.NewOwnerId, out var newOwnerId) || newOwnerId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Verify current owner
        if (study.OwnerId != currentOwnerId)
            return Result.Failure<StudyDto>(StudyErrors.OnlyOwnerCanTransfer);

        // Transfer ownership
        var result = study.TransferOwnership(newOwnerId);
        if (result.IsFailure)
            return Result.Failure<StudyDto>(result.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(study.ToDto());
    }
}
