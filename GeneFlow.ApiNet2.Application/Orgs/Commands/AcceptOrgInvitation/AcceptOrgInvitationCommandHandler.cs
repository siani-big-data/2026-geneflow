using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.AcceptOrgInvitation;

public sealed class AcceptOrgInvitationCommandHandler
    : ICommandHandler<AcceptOrgInvitationCommand, Result>
{
    private readonly IOrgRepository _orgRepository;
    private readonly IOrgInvitationRepository _invitationRepository;
    private readonly IOrgUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AcceptOrgInvitationCommandHandler(
        IOrgRepository orgRepository,
        IOrgInvitationRepository invitationRepository,
        IOrgUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        AcceptOrgInvitationCommand request,
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

        var acceptResult = invitation.Accept(actor);
        if (acceptResult.IsFailure)
        {
            _invitationRepository.Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return acceptResult;
        }

        var org = await _orgRepository.GetByIdAsync(invitation.OrgId, cancellationToken);
        if (org is null)
            return Result.Failure(OrgErrors.NotFound);

        var addResult = org.AddMember(actor, invitation.Role);
        if (addResult.IsFailure)
            return addResult;

        _orgRepository.Update(org);
        _invitationRepository.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
