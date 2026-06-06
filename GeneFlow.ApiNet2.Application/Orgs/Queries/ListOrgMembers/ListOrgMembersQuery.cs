using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Queries.ListOrgMembers;

public sealed record ListOrgMembersQuery(string OrgId)
    : IQuery<Result<IReadOnlyList<OrgMemberDto>>>, IRequireAuthentication;
