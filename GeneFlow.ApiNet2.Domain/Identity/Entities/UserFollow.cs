using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Identity.Entities;

/// <summary>
/// Represents a user following another user (social graph edge).
/// </summary>
public sealed class UserFollow : Entity<Guid>
{
    public UserId FollowerId { get; private set; } = null!;
    public UserId FolloweeId { get; private set; } = null!;
    public DateTime FollowedAt { get; private set; }

    private UserFollow() : base() { }

    private UserFollow(Guid id, UserId followerId, UserId followeeId) : base(id)
    {
        FollowerId = followerId;
        FolloweeId = followeeId;
        FollowedAt = DateTime.UtcNow;
    }

    public static UserFollow Create(UserId followerId, UserId followeeId)
    {
        if (followerId == followeeId)
            throw new InvalidOperationException("A user cannot follow themselves.");

        return new UserFollow(Guid.NewGuid(), followerId, followeeId);
    }
}
