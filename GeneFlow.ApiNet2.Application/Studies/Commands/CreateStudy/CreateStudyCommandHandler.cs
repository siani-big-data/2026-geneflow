using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.CreateStudy;

/// <summary>
/// Handler for CreateStudyCommand.
/// </summary>
public sealed class CreateStudyCommandHandler
    : ICommandHandler<CreateStudyCommand, Result<StudyDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly IOrgRepository _orgRepository;

    public CreateStudyCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator,
        IOrgRepository orgRepository)
    {
        _studyRepository = studyRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
        _orgRepository = orgRepository;
    }

    public async Task<Result<StudyDto>> Handle(
        CreateStudyCommand request,
        CancellationToken cancellationToken)
    {
        // Parse user ID
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<StudyDto>(StudyErrors.InvalidUserId);

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

        // Generate study ID
        var sequenceId = await _sequenceGenerator.NextAsync(StudyId.SequenceName, cancellationToken);
        var studyId = StudyId.FromSequence(sequenceId);

        // Resolve owner: personal (User) or organisation (Org).
        var isOrgOwned = string.Equals(request.OwnerType, "Org", StringComparison.OrdinalIgnoreCase);

        Result<Study> studyResult;

        if (isOrgOwned)
        {
            if (string.IsNullOrWhiteSpace(request.OwnerHandle))
                return Result.Failure<StudyDto>(StudyErrors.OrgOwnerHandleRequired);

            var org = await _orgRepository.GetByHandleAsync(request.OwnerHandle, cancellationToken);
            if (org is null)
                return Result.Failure<StudyDto>(StudyErrors.OrgOwnerNotFound);

            // Caller must be Owner or Admin of the org to publish a study under it.
            var member = org.GetMember(userId);
            if (member is null || !member.Role.CanEditOrg)
                return Result.Failure<StudyDto>(StudyErrors.OrgOwnerInsufficientRole);

            studyResult = Study.CreateForOrg(
                studyId,
                org.Id,
                userId,
                titleResult.Value,
                description,
                researchField);
        }
        else
        {
            studyResult = Study.Create(
                studyId,
                userId,
                titleResult.Value,
                description,
                researchField);
        }

        if (studyResult.IsFailure)
            return Result.Failure<StudyDto>(studyResult.Error);

        var study = studyResult.Value;

        // Set optional fields
        if (!string.IsNullOrWhiteSpace(request.Institution))
        {
            var instResult = study.UpdateInstitution(request.Institution, userId);
            if (instResult.IsFailure)
                return Result.Failure<StudyDto>(instResult.Error);
        }

        if (!string.IsNullOrWhiteSpace(request.PrincipalInvestigator))
        {
            var piResult = study.UpdatePrincipalInvestigator(request.PrincipalInvestigator, userId);
            if (piResult.IsFailure)
                return Result.Failure<StudyDto>(piResult.Error);
        }

        // Add tags if provided
        if (request.Tags is { Count: > 0 })
        {
            foreach (var tag in request.Tags)
            {
                var tagResult = study.AddTag(tag, userId);
                if (tagResult.IsFailure)
                    return Result.Failure<StudyDto>(tagResult.Error);
            }
        }

        // Persist
        await _studyRepository.AddAsync(study, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(study.ToDto());
    }
}
