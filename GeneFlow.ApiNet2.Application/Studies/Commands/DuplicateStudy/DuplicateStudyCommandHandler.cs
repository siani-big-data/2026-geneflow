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
    /// <summary>Suffix appended to the duplicated study title.</summary>
    private const string CopyTitleSuffix = " (Copy)";

    /// <summary>
    /// Maximum length of the original title preserved when appending
    /// <see cref="CopyTitleSuffix"/>, so the resulting title fits within
    /// <see cref="StudyTitle"/> validation bounds.
    /// </summary>
    private const int MaxTitlePrefixLength = 90;

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
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        var originalStudy = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (originalStudy is null)
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        if (!originalStudy.IsMember(userId))
            return Result.Failure<StudyDto>(StudyErrors.NotFound);

        var originalTitle = originalStudy.Title.Value;
        var newTitleValue = originalTitle.Length > MaxTitlePrefixLength
            ? originalTitle[..MaxTitlePrefixLength] + CopyTitleSuffix
            : originalTitle + CopyTitleSuffix;

        var titleResult = StudyTitle.Create(newTitleValue);
        if (titleResult.IsFailure)
            return Result.Failure<StudyDto>(titleResult.Error);

        var sequenceId = await _sequenceGenerator.NextAsync(StudyId.SequenceName, cancellationToken);
        var newStudyId = StudyId.FromSequence(sequenceId);

        var studyResult = Study.Create(
            newStudyId,
            userId,
            titleResult.Value,
            originalStudy.Description,
            originalStudy.ResearchField);

        if (studyResult.IsFailure)
            return Result.Failure<StudyDto>(studyResult.Error);

        var newStudy = studyResult.Value;

        if (!string.IsNullOrWhiteSpace(originalStudy.Institution))
        {
            newStudy.UpdateInstitution(originalStudy.Institution, userId);
        }

        if (!string.IsNullOrWhiteSpace(originalStudy.PrincipalInvestigator))
        {
            newStudy.UpdatePrincipalInvestigator(originalStudy.PrincipalInvestigator, userId);
        }

        foreach (var tag in originalStudy.Tags)
        {
            newStudy.AddTag(tag, userId);
        }

        var newSettings = StudySettings.Create(
            originalStudy.Settings.AllowPublicComments,
            originalStudy.Settings.AllowDataDownload,
            originalStudy.Settings.RequireApprovalToJoin);
        newStudy.UpdateSettings(newSettings, userId);

        await _studyRepository.AddAsync(newStudy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(newStudy.ToDto());
    }
}
