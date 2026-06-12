using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyInvitations;

/// <summary>
/// Query to get all invitations for a study.
/// Requires Admin role or higher.
/// </summary>
public sealed record GetStudyInvitationsQuery(
    string StudyId,
    string UserId,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<PagedList<StudyInvitationDto>>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Admin;
}
