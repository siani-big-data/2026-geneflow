using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.SendInvitation;

/// <summary>
/// Handler for SendInvitationCommand.
/// </summary>
public sealed class SendInvitationCommandHandler
    : ICommandHandler<SendInvitationCommand, Result<StudyInvitationDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyInvitationRepository _invitationRepository;
    private readonly IStudyUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;

    public SendInvitationCommandHandler(
        IStudyRepository studyRepository,
        IStudyInvitationRepository invitationRepository,
        IStudyUnitOfWork unitOfWork,
        ISequenceGenerator sequenceGenerator)
    {
        _studyRepository = studyRepository;
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<Result<StudyInvitationDto>> Handle(
        SendInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyInvitationDto>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.InvitedByUserId, out var invitedBy) || invitedBy is null)
            return Result.Failure<StudyInvitationDto>(StudyErrors.InvalidUserId);

        // Validate role
        var role = StudyRole.FromId(request.RoleId);
        if (role is null || role == StudyRole.Owner)
            return Result.Failure<StudyInvitationDto>(StudyErrors.InvalidRole);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyInvitationDto>(StudyErrors.NotFound);

        // Check permission (owner or admin can invite)
        var member = study.GetMember(invitedBy);
        if (member is null || !member.Role.CanManageMembers)
            return Result.Failure<StudyInvitationDto>(StudyErrors.InsufficientPermissions);

        // Check for existing pending invitation
        if (await _invitationRepository.HasPendingInvitationAsync(studyId, request.Email, cancellationToken))
            return Result.Failure<StudyInvitationDto>(StudyErrors.InvitationAlreadyExists);

        // Generate invitation ID
        var sequenceId = await _sequenceGenerator.NextAsync(StudyInvitationId.SequenceName, cancellationToken);
        var invitationId = StudyInvitationId.FromSequence(sequenceId);

        // Create invitation
        var invitationResult = StudyInvitation.Create(
            invitationId,
            studyId,
            request.Email,
            role,
            invitedBy,
            request.Message);

        if (invitationResult.IsFailure)
            return Result.Failure<StudyInvitationDto>(invitationResult.Error);

        var invitation = invitationResult.Value;

        // Persist
        await _invitationRepository.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(invitation.ToDto());
    }
}
