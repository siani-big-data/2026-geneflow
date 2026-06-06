using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Discussions;

public static class ReactionErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Reaction.NotFound", "Reaction was not found.");

    public static readonly Error EmojiRequired =
        Error.Validation("Reaction.EmojiRequired", "Emoji is required.");

    public static readonly Error EmojiTooLong =
        Error.Validation("Reaction.EmojiTooLong", "Emoji must be 16 characters or less.");

    public static readonly Error AlreadyReacted =
        Error.Conflict("Reaction.AlreadyReacted", "User has already reacted with this emoji.");
}
