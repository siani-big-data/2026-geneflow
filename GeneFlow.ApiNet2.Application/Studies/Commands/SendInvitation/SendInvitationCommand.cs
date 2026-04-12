using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.SendInvitation;

/// <summary>
/// Command to send a study invitation.
/// </summary>
public sealed record SendInvitationCommand(
    string StudyId,
    string InvitedByUserId,
    string Email,
    int RoleId,
    string? Message = null) : ICommand<Result<StudyInvitationDto>>;
