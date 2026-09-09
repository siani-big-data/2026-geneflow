using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Identity.Queries.IsUserFollowed;

public sealed class IsUserFollowedQueryHandler
    : IQueryHandler<IsUserFollowedQuery, Result<bool>>
{
    private readonly IUserRepository _userRepository;

    public IsUserFollowedQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> Handle(
        IsUserFollowedQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.FollowerId, out var followerId) || followerId is null)
            return Result.Failure<bool>(UserErrors.InvalidUserId);

        if (!UserId.TryParse(request.FolloweeId, out var followeeId) || followeeId is null)
            return Result.Failure<bool>(UserErrors.InvalidUserId);

        var isFollowing = await _userRepository.IsFollowingAsync(followerId, followeeId, cancellationToken);
        return Result.Success(isFollowing);
    }
}
