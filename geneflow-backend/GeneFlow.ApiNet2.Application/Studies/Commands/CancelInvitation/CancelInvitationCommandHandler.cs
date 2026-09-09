using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.CancelInvitation;

/// <summary>
/// Handler for CancelInvitationCommand.
/// </summary>
public sealed class CancelInvitationCommandHandler
    : ICommandHandler<CancelInvitationCommand, Result>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyInvitationRepository _invitationRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public CancelInvitationCommandHandler(
        IStudyRepository studyRepository,
        IStudyInvitationRepository invitationRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _studyRepository = studyRepository;
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        CancelInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(StudyErrors.NotFound);

        if (!StudyInvitationId.TryParse(request.InvitationId, out var invitationId) || invitationId is null)
            return Result.Failure(StudyErrors.InvitationNotFound);

        if (!UserId.TryParse(request.CancelledByUserId, out var cancelledBy) || cancelledBy is null)
            return Result.Failure(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure(StudyErrors.NotFound);

        // Check permission (owner or admin can cancel)
        var member = study.GetMember(cancelledBy);
        if (member is null || !member.Role.CanManageMembers)
            return Result.Failure(StudyErrors.InsufficientPermissions);

        // Get invitation
        var invitation = await _invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null || invitation.StudyId != studyId)
            return Result.Failure(StudyErrors.InvitationNotFound);

        // Cancel invitation
        var cancelResult = invitation.Cancel();
        if (cancelResult.IsFailure)
            return cancelResult;

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
