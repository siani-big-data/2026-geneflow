using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetMyInvitations;

/// <summary>
/// Handler for GetMyInvitationsQuery.
/// </summary>
public sealed class GetMyInvitationsQueryHandler
    : IQueryHandler<GetMyInvitationsQuery, Result<PagedList<StudyInvitationDto>>>
{
    private readonly IStudyInvitationRepository _invitationRepository;

    public GetMyInvitationsQueryHandler(IStudyInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<Result<PagedList<StudyInvitationDto>>> Handle(
        GetMyInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure<PagedList<StudyInvitationDto>>(StudyErrors.InvalidEmail);

        var invitations = await _invitationRepository.GetPendingByEmailAsync(
            request.Email,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = invitations.Items.ToDtos();

        return Result.Success(PagedList<StudyInvitationDto>.Create(
            dtos,
            invitations.PageNumber,
            invitations.PageSize,
            invitations.TotalCount));
    }
}
