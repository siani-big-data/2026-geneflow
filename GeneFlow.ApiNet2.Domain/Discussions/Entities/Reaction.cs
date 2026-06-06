using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Discussions.Entities;

/// <summary>
/// A user's emoji reaction to a comment. Uniqueness is enforced at the
/// database level on (CommentId, UserId, Emoji).
/// </summary>
public sealed class Reaction : AggregateRoot<Guid>
{
    public const int MaxEmojiLength = 16;

    public Guid CommentId { get; private set; }
    public UserId UserId { get; private set; } = null!;
    public string Emoji { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Reaction() : base() { }

    private Reaction(Guid id, Guid commentId, UserId userId, string emoji) : base(id)
    {
        CommentId = commentId;
        UserId = userId;
        Emoji = emoji;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Reaction> Create(Guid commentId, UserId userId, string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            return Result.Failure<Reaction>(ReactionErrors.EmojiRequired);

        if (emoji.Length > MaxEmojiLength)
            return Result.Failure<Reaction>(ReactionErrors.EmojiTooLong);

        var reaction = new Reaction(Guid.NewGuid(), commentId, userId, emoji);
        reaction.RaiseDomainEvent(new ReactionAddedEvent(reaction.Id, commentId, userId, emoji));
        return reaction;
    }

    /// <summary>
    /// Raises the removal event. Repository is responsible for actually
    /// deleting the row; this just records the intent so the WatchNotifier
    /// and any future projections see it.
    /// </summary>
    public void MarkRemoved()
    {
        RaiseDomainEvent(new ReactionRemovedEvent(CommentId, UserId, Emoji));
    }
}
