using GeneFlow.ApiNet2.Application.Identity.Queries.GetFollowers;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetFollowing;

/// <summary>
/// Query to list the users that a user is following.
/// </summary>
public sealed record GetFollowingQuery(
    string UserId,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<PagedList<FollowUserDto>>>;
