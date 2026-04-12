using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Application.Studies.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyInvitations;

/// <summary>
/// Handler for GetStudyInvitationsQuery.
/// </summary>
public sealed class GetStudyInvitationsQueryHandler
    : IQueryHandler<GetStudyInvitationsQuery, Result<PagedList<StudyInvitationDto>>>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IStudyInvitationRepository _invitationRepository;

    public GetStudyInvitationsQueryHandler(
        IStudyRepository studyRepository,
        IStudyInvitationRepository invitationRepository)
    {
        _studyRepository = studyRepository;
        _invitationRepository = invitationRepository;
    }

    public async Task<Result<PagedList<StudyInvitationDto>>> Handle(
        GetStudyInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        // Parse IDs
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<PagedList<StudyInvitationDto>>(StudyErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<PagedList<StudyInvitationDto>>(StudyErrors.InvalidUserId);

        // Get study
        var study = await _studyRepository.GetByIdAsync(studyId, cancellationToken);
        if (study is null)
            return Result.Failure<PagedList<StudyInvitationDto>>(StudyErrors.NotFound);

        // Check permission (must be member to view invitations)
        var member = study.GetMember(userId);
        if (member is null || !member.Role.CanManageMembers)
            return Result.Failure<PagedList<StudyInvitationDto>>(StudyErrors.InsufficientPermissions);

        // Get invitations
        var invitations = await _invitationRepository.GetByStudyAsync(
            studyId,
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
