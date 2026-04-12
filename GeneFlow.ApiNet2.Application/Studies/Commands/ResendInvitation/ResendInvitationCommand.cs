using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ResendInvitation;

/// <summary>
/// Command to resend a study invitation.
/// </summary>
public sealed record ResendInvitationCommand(
    string StudyId,
    string InvitationId,
    string ResentByUserId) : ICommand<Result<StudyInvitationDto>>;
