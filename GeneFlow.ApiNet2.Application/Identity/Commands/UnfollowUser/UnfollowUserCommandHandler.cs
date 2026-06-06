using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Application.Identity.Commands.UnfollowUser;

/// <summary>
/// Handler for UnfollowUserCommand.
/// </summary>
public sealed class UnfollowUserCommandHandler
    : ICommandHandler<UnfollowUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UnfollowUserCommandHandler(
        IUserRepository userRepository,
        IUserUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result> Handle(
        UnfollowUserCommand request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.FollowerId, out var followerId) || followerId is null)
            return Result.Failure(UserErrors.InvalidUserId);

        if (!UserId.TryParse(request.FolloweeId, out var followeeId) || followeeId is null)
            return Result.Failure(UserErrors.InvalidUserId);

        if (!await _userRepository.IsFollowingAsync(followerId, followeeId, cancellationToken))
            return Result.Failure(UserErrors.NotFollowing);

        await _userRepository.RemoveFollowAsync(followerId, followeeId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventDispatcher.DispatchAsync(
            new UserUnfollowedEvent(followerId, followeeId),
            cancellationToken);

        return Result.Success();
    }
}
