using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudySettings;

/// <summary>
/// Handler for UpdateStudySettingsCommand.
/// </summary>
public sealed class UpdateStudySettingsCommandHandler
    : ICommandHandler<UpdateStudySettingsCommand, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public UpdateStudySettingsCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyDto>> Handle(
        UpdateStudySettingsCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Check permission (owner or admin can update settings)
        var member = study.GetMember(userId);
        if (member is null || !member.Role.CanManageMembers)
            return Result.Failure<StudyDto>(StudyErrors.InsufficientPermissions);

        // Create new settings
        var settings = StudySettings.Create(
            request.AllowPublicComments,
            request.AllowDataDownload,
            request.RequireApprovalToJoin);

        // Update settings
        var result = study.UpdateSettings(settings, userId);
        if (result.IsFailure)
            return Result.Failure<StudyDto>(result.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(study.ToDto());
    }
}
