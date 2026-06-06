using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.IsUserFollowed;

/// <summary>
/// Query to check whether the current user follows the target user.
/// </summary>
public sealed record IsUserFollowedQuery(
    string FollowerId,
    string FolloweeId) : IQuery<Result<bool>>, IRequireAuthentication;
