using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Queries.GetOrgByHandle;

public sealed record GetOrgByHandleQuery(string Handle) : IQuery<Result<OrgDto>>;
