using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.ChangeOrgMemberRole;

public sealed record ChangeOrgMemberRoleCommand(
    string OrgId,
    string TargetUserId,
    string NewRole) : ICommand<Result>, IRequireAuthentication;
