using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.Application.Orgs.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.InviteOrgMember;

public sealed class InviteOrgMemberCommandHandler
    : ICommandHandler<InviteOrgMemberCommand, Result<OrgInvitationDto>>
{
    private readonly IOrgRepository _orgRepository;
    private readonly IOrgInvitationRepository _invitationRepository;
    private readonly IOrgUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public InviteOrgMemberCommandHandler(
        IOrgRepository orgRepository,
        IOrgInvitationRepository invitationRepository,
        IOrgUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<OrgInvitationDto>> Handle(
        InviteOrgMemberCommand request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.UserId;
        if (actor is null)
            return Result.Failure<OrgInvitationDto>(OrgErrors.Forbidden);

        if (!OrgId.TryParse(request.OrgId, out var orgId) || orgId is null)
            return Result.Failure<OrgInvitationDto>(OrgErrors.NotFound);

        var org = await _orgRepository.GetByIdAsync(orgId, cancellationToken);
        if (org is null)
            return Result.Failure<OrgInvitationDto>(OrgErrors.NotFound);

        var actorMember = org.GetMember(actor);
        if (actorMember is null || !actorMember.Role.CanManageMembers)
            return Result.Failure<OrgInvitationDto>(OrgErrors.Forbidden);

        var role = Enumeration<OrgRole>.FromName(request.Role);
        if (role is null)
            return Result.Failure<OrgInvitationDto>(OrgErrors.Forbidden);

        // Best-effort resolve email → existing user.
        UserId? invitedUserId = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var user = await _userRepository.GetByEmailStringAsync(request.Email, cancellationToken);
            if (user is not null)
                invitedUserId = user.Id;
        }

        var sequence = await _invitationRepository.GetNextSequenceValueAsync(cancellationToken);
        var invitationId = OrgInvitationId.FromSequence(sequence);

        var createResult = OrgInvitation.Create(invitationId, orgId, request.Email, invitedUserId, role);
        if (createResult.IsFailure)
            return Result.Failure<OrgInvitationDto>(createResult.Error);

        var invitation = createResult.Value;
        await _invitationRepository.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(invitation.ToDto(org.Handle, org.Name));
    }
}
