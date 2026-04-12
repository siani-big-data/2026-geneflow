using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.DeclineInvitation;

/// <summary>
/// Handler for DeclineInvitationCommand.
/// </summary>
public sealed class DeclineInvitationCommandHandler
    : ICommandHandler<DeclineInvitationCommand, Result>
{
    private readonly IStudyInvitationRepository _invitationRepository;
    private readonly IStudyUnitOfWork _unitOfWork;

    public DeclineInvitationCommandHandler(
        IStudyInvitationRepository invitationRepository,
        IStudyUnitOfWork unitOfWork)
    {
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeclineInvitationCommand request,
        CancellationToken cancellationToken)
    {
        // Get invitation by token
        var invitation = await _invitationRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (invitation is null)
            return Result.Failure(StudyErrors.InvitationNotFound);

        // Decline invitation
        var declineResult = invitation.Decline();
        if (declineResult.IsFailure)
            return declineResult;

        // Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
