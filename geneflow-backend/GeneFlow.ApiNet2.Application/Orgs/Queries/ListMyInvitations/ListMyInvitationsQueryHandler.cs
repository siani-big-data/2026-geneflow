using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.Application.Orgs.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Queries.ListMyInvitations;

public sealed class ListMyInvitationsQueryHandler
    : IQueryHandler<ListMyInvitationsQuery, Result<IReadOnlyList<OrgInvitationDto>>>
{
    private readonly IOrgInvitationRepository _invitationRepository;
    private readonly IOrgRepository _orgRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public ListMyInvitationsQueryHandler(
        IOrgInvitationRepository invitationRepository,
        IOrgRepository orgRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _invitationRepository = invitationRepository;
        _orgRepository = orgRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<OrgInvitationDto>>> Handle(
        ListMyInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.UserId;
        if (actor is null)
            return Result.Failure<IReadOnlyList<OrgInvitationDto>>(OrgErrors.Forbidden);

        var user = await _userRepository.GetByIdAsync(actor, cancellationToken);
        if (user is null)
            return Result.Failure<IReadOnlyList<OrgInvitationDto>>(OrgErrors.Forbidden);

        var email = user.Email.Value;
        var invitations = await _invitationRepository.ListPendingForUserAsync(actor, email, cancellationToken);

        // Enrich with org handle/name.
        var orgIds = invitations.Select(i => i.OrgId).Distinct().ToList();
        var orgMap = new Dictionary<string, Org>();
        foreach (var orgId in orgIds)
        {
            var org = await _orgRepository.GetByIdAsync(orgId, cancellationToken);
            if (org is not null)
                orgMap[orgId.ToString()] = org;
        }

        var dtos = invitations.Select(inv =>
        {
            orgMap.TryGetValue(inv.OrgId.ToString(), out var org);
            return inv.ToDto(org?.Handle, org?.Name);
        }).ToList();

        return Result.Success<IReadOnlyList<OrgInvitationDto>>(dtos);
    }
}
