using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyInvitations;

/// <summary>
/// Query to get all invitations for a study.
/// </summary>
public sealed record GetStudyInvitationsQuery(
    string StudyId,
    string UserId,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<PagedList<StudyInvitationDto>>>;
