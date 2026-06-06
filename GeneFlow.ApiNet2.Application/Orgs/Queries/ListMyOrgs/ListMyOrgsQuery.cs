using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Queries.ListMyOrgs;

public sealed record ListMyOrgsQuery()
    : IQuery<Result<IReadOnlyList<OrgMembershipDto>>>, IRequireAuthentication;
