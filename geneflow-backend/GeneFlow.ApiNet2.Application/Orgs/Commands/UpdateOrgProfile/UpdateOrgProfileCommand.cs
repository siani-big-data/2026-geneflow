using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.UpdateOrgProfile;

public sealed record UpdateOrgProfileCommand(
    string OrgId,
    string Name,
    string? Description,
    string? AvatarUrl,
    string? WebsiteUrl,
    string? Location) : ICommand<Result<OrgDto>>, IRequireAuthentication;
