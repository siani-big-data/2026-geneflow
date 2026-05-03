using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.DuplicateStudy;

/// <summary>
/// Handler for DuplicateStudyCommand.
/// Creates a copy of an existing study with a new ID.
/// </summary>
public sealed class DuplicateStudyCommandHandler
    : ICommandHandler<DuplicateStudyCommand, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;

    public DuplicateStudyCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<Result<StudyDto>> Handle(
        DuplicateStudyCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

        // Parse study ID
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Get original study
        var originalStudy = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (originalStudy is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Check if user has access (must be a member)
        if (!originalStudy.IsMember(userId))
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        // Create new title with "(Copy)" suffix
        var newTitleValue = originalStudy.Title.Value.Length > 90
            ? originalStudy.Title.Value[..90] + " (Copy)"
            : originalStudy.Title.Value + " (Copy)";

        var titleResult = StudyTitle.Create(newTitleValue);
        if (titleResult.IsFailure)
            return Result.Failure<StudyDto>(titleResult.Error);

        // Generate new study ID
        var sequenceId = await _sequenceGenerator.NextAsync(StudyId.SequenceName, cancellationToken);
        var newStudyId = StudyId.FromSequence(sequenceId);

        // Create new study (starts as Draft, user becomes owner)
        var studyResult = Study.Create(
            newStudyId,
            userId,
            titleResult.Value,
            originalStudy.Description,
            originalStudy.ResearchField);

        if (studyResult.IsFailure)
            return Result.Failure<StudyDto>(studyResult.Error);

        var newStudy = studyResult.Value;

        // Copy optional fields
        if (!string.IsNullOrWhiteSpace(originalStudy.Institution))
        {
            newStudy.UpdateInstitution(originalStudy.Institution, userId);
        }

        if (!string.IsNullOrWhiteSpace(originalStudy.PrincipalInvestigator))
        {
            newStudy.UpdatePrincipalInvestigator(originalStudy.PrincipalInvestigator, userId);
        }

        // Copy tags
        foreach (var tag in originalStudy.Tags)
        {
            newStudy.AddTag(tag, userId);
        }

        // Copy settings
        var newSettings = StudySettings.Create(
            originalStudy.Settings.AllowPublicComments,
            originalStudy.Settings.AllowDataDownload,
            originalStudy.Settings.RequireApprovalToJoin);
        newStudy.UpdateSettings(newSettings, userId);

        // Persist
        await _studyRepository.AddAsync(newStudy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(newStudy.ToDto());
    }
}
