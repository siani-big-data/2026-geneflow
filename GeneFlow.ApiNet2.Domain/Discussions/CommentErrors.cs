using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Discussions;

public static class CommentErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Comment.NotFound", "Comment was not found.");

    public static Error NotFoundById(Guid id) =>
        Error.NotFound("Comment.NotFoundById", $"Comment with ID '{id}' was not found.");

    public static readonly Error BodyRequired =
        Error.Validation("Comment.BodyRequired", "Comment body is required.");

    public static readonly Error BodyTooLong =
        Error.Validation("Comment.BodyTooLong", "Comment body must be 10,000 characters or less.");

    public static readonly Error AlreadyDeleted =
        Error.Validation("Comment.AlreadyDeleted", "Comment has already been deleted.");

    public static readonly Error NotAuthor =
        Error.Forbidden("Comment.NotAuthor", "Only the author can modify this comment.");

    public static readonly Error InsufficientPermissions =
        Error.Forbidden("Comment.InsufficientPermissions", "You don't have permission to perform this action on the comment.");
}
