using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.DeclineInvitation;

/// <summary>
/// Command to decline a study invitation.
/// </summary>
public sealed record DeclineInvitationCommand(string Token) : ICommand<Result>;
