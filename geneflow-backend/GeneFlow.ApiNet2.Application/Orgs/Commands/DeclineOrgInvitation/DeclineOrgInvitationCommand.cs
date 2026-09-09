using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.DeclineOrgInvitation;

public sealed record DeclineOrgInvitationCommand(string InvitationId)
    : ICommand<Result>, IRequireAuthentication;
