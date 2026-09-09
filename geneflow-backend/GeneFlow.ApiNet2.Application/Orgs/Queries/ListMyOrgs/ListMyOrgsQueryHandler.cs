using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.Application.Orgs.Mappings;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Queries.ListMyOrgs;

public sealed class ListMyOrgsQueryHandler
    : IQueryHandler<ListMyOrgsQuery, Result<IReadOnlyList<OrgMembershipDto>>>
{
    private readonly IOrgRepository _orgRepository;
    private readonly ICurrentUserService _currentUser;

    public ListMyOrgsQueryHandler(
        IOrgRepository orgRepository,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<OrgMembershipDto>>> Handle(
        ListMyOrgsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.UserId;
        if (actor is null)
            return Result.Failure<IReadOnlyList<OrgMembershipDto>>(OrgErrors.Forbidden);

        var orgs = await _orgRepository.ListByMemberAsync(actor, cancellationToken);
        var dtos = orgs.Select(o => o.ToMembershipDto(actor)).ToList();

        return Result.Success<IReadOnlyList<OrgMembershipDto>>(dtos);
    }
}
