using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.GetFollowers;

/// <summary>
/// Query to list the followers of a user.
/// </summary>
public sealed record GetFollowersQuery(
    string UserId,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<PagedList<FollowUserDto>>>;
