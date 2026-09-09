using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ResendInvitation;

/// <summary>
/// Handler for ResendInvitationCommand.
/// </summary>
public sealed class ResendInvitationCommandHandler
    : ICommandHandler<ResendInvitationCommand, Result<StudyInvitationDto>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyInvitationRepository _invitationRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public ResendInvitationCommandHandler(
        IStudyRepository studyRepository,
        IStudyInvitationRepository invitationRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StudyInvitationDto>> Handle(
        ResendInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<StudyInvitationDto>(StudyErrors.NotFound);

        if (!StudyInvitationId.TryParse(request.InvitationId, out var invitationId) || invitationId is null)
            return Result.Failure<StudyInvitationDto>(StudyErrors.InvitationNotFound);

        if (!UserId.TryParse(request.ResentByUserId, out var resentBy) || resentBy is null)
            return Result.Failure<StudyInvitationDto>(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<StudyInvitationDto>(StudyErrors.NotFound);

        // Check permission (owner or admin can resend)
        var member = study.GetMember(resentBy);
        if (member is null || !member.Role.CanManageMembers)
            return Result.Failure<StudyInvitationDto>(StudyErrors.InsufficientPermissions);

        // Get invitation
        var invitation = await _invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null || invitation.StudyId != studyId)
            return Result.Failure<StudyInvitationDto>(StudyErrors.InvitationNotFound);

        // Resend invitation
        var resendResult = invitation.Resend();
        if (resendResult.IsFailure)
            return Result.Failure<StudyInvitationDto>(resendResult.Error);

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(invitation.ToDto());
    }
}
