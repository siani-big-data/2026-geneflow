using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.CancelInvitation;

/// <summary>
/// Command to cancel a study invitation.
/// </summary>
public sealed record CancelInvitationCommand(
    string StudyId,
    string InvitationId,
    string CancelledByUserId) : ICommand<Result>;
