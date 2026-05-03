using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetMyInvitations;

/// <summary>
/// Query to get pending invitations for the current user.
/// Requires authentication.
/// </summary>
public sealed record GetMyInvitationsQuery(
    string Email,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<PagedList<StudyInvitationDto>>>, IRequireAuthentication;
