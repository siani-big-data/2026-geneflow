using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.Application.Orgs.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Queries.ListOrgMembers;

public sealed class ListOrgMembersQueryHandler
    : IQueryHandler<ListOrgMembersQuery, Result<IReadOnlyList<OrgMemberDto>>>
{
    private readonly IOrgRepository _orgRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public ListOrgMembersQueryHandler(
        IOrgRepository orgRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUser)
    {
        _orgRepository = orgRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<OrgMemberDto>>> Handle(
        ListOrgMembersQuery request,
        CancellationToken cancellationToken)
    {
        var actor = _currentUser.UserId;
        if (actor is null)
            return Result.Failure<IReadOnlyList<OrgMemberDto>>(OrgErrors.Forbidden);

        if (!OrgId.TryParse(request.OrgId, out var orgId) || orgId is null)
            return Result.Failure<IReadOnlyList<OrgMemberDto>>(OrgErrors.NotFound);

        var org = await _orgRepository.GetByIdAsync(orgId, cancellationToken);
        if (org is null)
            return Result.Failure<IReadOnlyList<OrgMemberDto>>(OrgErrors.NotFound);

        if (org.Visibility == OrgVisibility.Private && !org.IsMember(actor))
            return Result.Failure<IReadOnlyList<OrgMemberDto>>(OrgErrors.Forbidden);

        var members = await _orgRepository.ListMembersAsync(orgId, cancellationToken);

        // Best-effort enrichment with usernames/avatars.
        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var dtos = members.Select(m =>
        {
            userMap.TryGetValue(m.UserId, out var user);
            return m.ToDto(user?.Username.Value, null);
        }).ToList();

        return Result.Success<IReadOnlyList<OrgMemberDto>>(dtos);
    }
}
