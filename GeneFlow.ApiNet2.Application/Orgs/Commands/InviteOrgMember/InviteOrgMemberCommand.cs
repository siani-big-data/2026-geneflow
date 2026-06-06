using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Orgs.Dtos;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.InviteOrgMember;

public sealed record InviteOrgMemberCommand(
    string OrgId,
    string Email,
    string Role) : ICommand<Result<OrgInvitationDto>>, IRequireAuthentication;
