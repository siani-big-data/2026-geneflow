using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.ChangeOrgMemberRole;

public sealed class ChangeOrgMemberRoleCommandHandler
    : ICommandHandler<ChangeOrgMemberRoleCommand, Result>
{
    private readonly IOrgRepository _orgRepository;
    private readonly IOrgUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ChangeOrgMemberRoleCommandHandler(
        IOrgRepository orgRepository,
        IOrgUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        ChangeOrgMemberRoleCommand request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.UserId;
        if (actor is null)
            return Result.Failure(OrgErrors.Forbidden);

        if (!OrgId.TryParse(request.OrgId, out var orgId) || orgId is null)
            return Result.Failure(OrgErrors.NotFound);

        if (!UserId.TryParse(request.TargetUserId, out var targetUserId) || targetUserId is null)
            return Result.Failure(OrgErrors.MemberNotFound);

        var newRole = Enumeration<OrgRole>.FromName(request.NewRole);
        if (newRole is null)
            return Result.Failure(OrgErrors.Forbidden);

        var org = await _orgRepository.GetByIdAsync(orgId, cancellationToken);
        if (org is null)
            return Result.Failure(OrgErrors.NotFound);

        var actorMember = org.GetMember(actor);
        if (actorMember is null || actorMember.Role != OrgRole.Owner)
            return Result.Failure(OrgErrors.Forbidden);

        var result = org.ChangeMemberRole(targetUserId, newRole, actor);
        if (result.IsFailure)
            return result;

        _orgRepository.Update(org);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
