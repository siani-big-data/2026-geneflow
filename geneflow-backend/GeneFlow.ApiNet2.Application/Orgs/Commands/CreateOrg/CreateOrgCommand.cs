using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.CreateOrg;

public sealed record CreateOrgCommand(
    string Handle,
    string Name,
    string? Description,
    string? Visibility) : ICommand<Result<OrgDto>>, IRequireAuthentication;
