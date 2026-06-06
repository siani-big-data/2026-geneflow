using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Orgs.Commands.AcceptOrgInvitation;

public sealed record AcceptOrgInvitationCommand(string InvitationId)
    : ICommand<Result>, IRequireAuthentication;
