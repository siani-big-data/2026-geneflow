using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.RemoveOrgMember;

public sealed record RemoveOrgMemberCommand(
    string OrgId,
    string TargetUserId) : ICommand<Result>, IRequireAuthentication;
