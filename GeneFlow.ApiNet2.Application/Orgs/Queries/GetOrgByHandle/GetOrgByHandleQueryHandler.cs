using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.Application.Orgs.Mappings;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Queries.GetOrgByHandle;

public sealed class GetOrgByHandleQueryHandler
    : IQueryHandler<GetOrgByHandleQuery, Result<OrgDto>>
{
    private readonly IOrgRepository _orgRepository;
    private readonly ICurrentUserService _currentUser;

    public GetOrgByHandleQueryHandler(
        IOrgRepository orgRepository,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<OrgDto>> Handle(
        GetOrgByHandleQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Handle))
            return Result.Failure<OrgDto>(OrgErrors.NotFound);

        var org = await _orgRepository.GetByHandleAsync(request.Handle.Trim().ToLowerInvariant(), cancellationToken);
        if (org is null)
            return Result.Failure<OrgDto>(OrgErrors.NotFound);

        if (org.Visibility == OrgVisibility.Private)
        {
            var actor = _currentUser.UserId;
            if (actor is null || !org.IsMember(actor))
                return Result.Failure<OrgDto>(OrgErrors.NotFound);
        }

        return Result.Success(org.ToDto());
    }
}
