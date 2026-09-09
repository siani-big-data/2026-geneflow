using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.Application.Orgs.Mappings;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.UpdateOrgProfile;

public sealed class UpdateOrgProfileCommandHandler
    : ICommandHandler<UpdateOrgProfileCommand, Result<OrgDto>>
{
    private readonly IOrgRepository _orgRepository;
    private readonly IOrgUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public UpdateOrgProfileCommandHandler(
        IOrgRepository orgRepository,
        IOrgUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<OrgDto>> Handle(
        UpdateOrgProfileCommand request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.UserId;
        if (actor is null)
            return Result.Failure<OrgDto>(OrgErrors.Forbidden);

        if (!OrgId.TryParse(request.OrgId, out var orgId) || orgId is null)
            return Result.Failure<OrgDto>(OrgErrors.NotFound);

        var org = await _orgRepository.GetByIdAsync(orgId, cancellationToken);
        if (org is null)
            return Result.Failure<OrgDto>(OrgErrors.NotFound);

        var member = org.GetMember(actor);
        if (member is null || !member.Role.CanEditOrg)
            return Result.Failure<OrgDto>(OrgErrors.Forbidden);

        var result = org.UpdateProfile(
            request.Name,
            request.Description,
            request.AvatarUrl,
            request.WebsiteUrl,
            request.Location,
            actor);

        if (result.IsFailure)
            return Result.Failure<OrgDto>(result.Error);

        _orgRepository.Update(org);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(org.ToDto(actor));
    }
}
