using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.DeclineOrgInvitation;

public sealed class DeclineOrgInvitationCommandHandler
    : ICommandHandler<DeclineOrgInvitationCommand, Result>
{
    private readonly IOrgInvitationRepository _invitationRepository;
    private readonly IOrgUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeclineOrgInvitationCommandHandler(
        IOrgInvitationRepository invitationRepository,
        IOrgUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        DeclineOrgInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.UserId;
        if (actor is null)
            return Result.Failure(OrgErrors.Forbidden);

        if (!OrgInvitationId.TryParse(request.InvitationId, out var invitationId) || invitationId is null)
            return Result.Failure(OrgErrors.OrgInvitation.NotFound);

        var invitation = await _invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation is null)
            return Result.Failure(OrgErrors.OrgInvitation.NotFound);

        var declineResult = invitation.Decline(actor);
        _invitationRepository.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return declineResult;
    }
}
