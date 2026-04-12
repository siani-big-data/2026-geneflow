using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.AcceptInvitation;

/// <summary>
/// Command to accept a study invitation.
/// </summary>
public sealed record AcceptInvitationCommand(
    string Token,
    string UserId) : ICommand<Result<StudyDto>>;
