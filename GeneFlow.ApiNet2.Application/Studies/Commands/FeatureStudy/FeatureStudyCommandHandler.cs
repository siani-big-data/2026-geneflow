using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.FeatureStudy;

/// <summary>
/// Handler for FeatureStudyCommand.
/// Note: Admin permission check should be done at API layer or via authorization middleware.
/// </summary>
public sealed class FeatureStudyCommandHandler
    : ICommandHandler<FeatureStudyCommand, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public FeatureStudyCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyDto>> Handle(
        FeatureStudyCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.AdminUserId, out var adminUserId) || adminUserId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Feature or unfeature (isAdmin = true since this command is for admins only)
        Result result;
        if (request.IsFeatured)
        {
            result = study.Feature(adminUserId, isAdmin: true);
        }
        else
        {
            result = study.Unfeature(adminUserId, isAdmin: true);
        }

        if (result.IsFailure)
            return Result.Failure<StudyDto>(result.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(study.ToDto());
    }
}
