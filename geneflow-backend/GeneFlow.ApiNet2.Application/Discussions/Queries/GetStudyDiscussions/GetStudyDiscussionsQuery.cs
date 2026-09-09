using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Queries.GetStudyDiscussions;

public sealed record GetStudyDiscussionsQuery(
    string StudyId,
    string? UserId,
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? Category = null) : IQuery<Result<PagedList<DiscussionDto>>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;

    // Allow read access for non-members when the study is public; the
    // StudyMembershipBehavior handles that fall-through.
    StudyRole? IRequireStudyMembership.MinimumRole => null;
}
