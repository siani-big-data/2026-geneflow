using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.TransferStudyOwnership;

public sealed class TransferStudyOwnershipCommandHandler
    : ICommandHandler<TransferStudyOwnershipCommand, Result>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyUnitOfWork _studyUnitOfWork;
    private readonly IOrgRepository _orgRepository;
    private readonly ICurrentUserService _currentUser;

    public TransferStudyOwnershipCommandHandler(
        IStudyRepository studyRepository,
        IStudyUnitOfWork studyUnitOfWork,
        IOrgRepository orgRepository,
        ICurrentUserService currentUser)
    {
        _studyRepository = studyRepository;
        _studyUnitOfWork = studyUnitOfWork;
        _orgRepository = orgRepository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        TransferStudyOwnershipCommand request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.UserId;
        if (actor is null)
            return Result.Failure(OrgErrors.Forbidden);

        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure(StudyErrors.NotFound);

        var newOwnerType = Enumeration<StudyOwnerType>.FromName(request.NewOwnerType);
        if (newOwnerType is null)
            return Result.Failure(OrgErrors.Forbidden);

        var study = await _studyRepository.GetByIdWithMembersAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure(StudyErrors.NotFound);

        // Verify actor is permitted to transfer ownership of the current owner.
        if (study.OwnerType == StudyOwnerType.User)
        {
            // Current owner is a user: only that user can initiate transfer.
            if (!study.OwnerId.Equals(actor))
                return Result.Failure(OrgErrors.Forbidden);
        }
        else
        {
            // Current owner is an org: only Owner/Admin of that org can initiate.
            var currentOrgId = OrgId.Parse(study.OwnerId.ToString());
            var currentOrg = await _orgRepository.GetByIdAsync(currentOrgId, cancellationToken);
            if (currentOrg is null)
                return Result.Failure(OrgErrors.Forbidden);
            var actorMember = currentOrg.GetMember(actor);
            if (actorMember is null || !actorMember.Role.CanManageMembers)
                return Result.Failure(OrgErrors.Forbidden);
        }

        // If new owner is an Org, the actor must be Owner/Admin of that Org.
        if (newOwnerType == StudyOwnerType.Org)
        {
            if (!OrgId.TryParse(request.NewOwnerId, out var newOrgId) || newOrgId is null)
                return Result.Failure(OrgErrors.NotFound);

            var newOrg = await _orgRepository.GetByIdAsync(newOrgId, cancellationToken);
            if (newOrg is null)
                return Result.Failure(OrgErrors.NotFound);
            var newOrgMember = newOrg.GetMember(actor);
            if (newOrgMember is null || !newOrgMember.Role.CanManageMembers)
                return Result.Failure(OrgErrors.Forbidden);
        }

        var transferResult = study.TransferOwnership(newOwnerType, request.NewOwnerId);
        if (transferResult.IsFailure)
            return transferResult;

        _studyRepository.Update(study);
        await _studyUnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
