using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetInvitationByToken;

/// <summary>
/// Handler for GetInvitationByTokenQuery.
/// </summary>
public sealed class GetInvitationByTokenQueryHandler
    : IQueryHandler<GetInvitationByTokenQuery, Result<StudyInvitationDto>>
{
    private readonly IStudyInvitationRepository _invitationRepository;

    public GetInvitationByTokenQueryHandler(IStudyInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<Result<StudyInvitationDto>> Handle(
        GetInvitationByTokenQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure<StudyInvitationDto>(StudyErrors.InvitationNotFound);

        var invitation = await _invitationRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (invitation is null)
            return Result.Failure<StudyInvitationDto>(StudyErrors.InvitationNotFound);

        return Result.Success(invitation.ToDto());
    }
}
