using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudy;

/// <summary>
/// Handler for UpdateStudyCommand.
/// </summary>
public sealed class UpdateStudyCommandHandler
    : ICommandHandler<UpdateStudyCommand, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public UpdateStudyCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyDto>> Handle(
        UpdateStudyCommand request,
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

        // Check permission (owner or admin can update)
        var member = study.GetMember(userId);
        if (member is null || !member.Role.CanEditContent)
            return Result.Failure<StudyDto>(StudyErrors.InsufficientPermissions);

        // Validate research field
        var researchField = ResearchField.FromId(request.ResearchFieldId);
        if (researchField is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidResearchField);

        // Create title value object
        var titleResult = StudyTitle.Create(request.Title);
        if (titleResult.IsFailure)
            return Result.Failure<StudyDto>(titleResult.Error);

        // Create description value object (optional)
        StudyDescription? description = null;
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var descriptionResult = StudyDescription.Create(request.Description);
            if (descriptionResult.IsFailure)
                return Result.Failure<StudyDto>(descriptionResult.Error);
            description = descriptionResult.Value;
        }

        // Update study
        var updateResult = study.Update(
            titleResult.Value,
            description,
            researchField,
            userId);

        if (updateResult.IsFailure)
            return Result.Failure<StudyDto>(updateResult.Error);

        // Update institution
        var instResult = study.UpdateInstitution(request.Institution, userId);
        if (instResult.IsFailure)
            return Result.Failure<StudyDto>(instResult.Error);

        // Update principal investigator
        var piResult = study.UpdatePrincipalInvestigator(request.PrincipalInvestigator, userId);
        if (piResult.IsFailure)
            return Result.Failure<StudyDto>(piResult.Error);

        // Update tags
        var tagsResult = study.SetTags(request.Tags ?? [], userId);
        if (tagsResult.IsFailure)
            return Result.Failure<StudyDto>(tagsResult.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(study.ToDto());
    }
}
