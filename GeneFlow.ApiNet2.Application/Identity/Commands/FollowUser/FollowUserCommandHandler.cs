using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.FollowUser;

/// <summary>
/// Handler for FollowUserCommand.
/// </summary>
public sealed class FollowUserCommandHandler
    : ICommandHandler<FollowUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public FollowUserCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result> Handle(
        FollowUserCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.FollowerId, out var followerId) || followerId is null)
            return Result.Failure(UserErrors.InvalidUserId);

        if (!UserId.TryParse(request.FolloweeId, out var followeeId) || followeeId is null)
            return Result.Failure(UserErrors.InvalidUserId);

        if (followerId == followeeId)
            return Result.Failure(UserErrors.CannotFollowSelf);

        // Verify target user exists and is not deleted
        var followee = await _userRepository.GetByIdAsync(followeeId, cancellationToken);
        if (followee is null)
            return Result.Failure(UserErrors.UserNotFoundById(request.FolloweeId));

        if (await _userRepository.IsFollowingAsync(followerId, followeeId, cancellationToken))
            return Result.Failure(UserErrors.AlreadyFollowing);

        var follow = UserFollow.Create(followerId, followeeId);
        await _userRepository.AddFollowAsync(follow, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventDispatcher.DispatchAsync(
            new UserFollowedEvent(followerId, followeeId),
            cancellationToken);

        return Result.Success();
    }
}
