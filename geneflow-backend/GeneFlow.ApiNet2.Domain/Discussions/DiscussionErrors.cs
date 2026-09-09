using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Discussions;

public static class DiscussionErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Discussion.NotFound", "Discussion was not found.");

    public static Error NotFoundById(string id) =>
        Error.NotFound("Discussion.NotFoundById", $"Discussion with ID '{id}' was not found.");

    public static readonly Error TitleRequired =
        Error.Validation("Discussion.TitleRequired", "Discussion title is required.");

    public static readonly Error TitleTooLong =
        Error.Validation("Discussion.TitleTooLong", "Discussion title must be 200 characters or less.");

    public static readonly Error CategoryTooLong =
        Error.Validation("Discussion.CategoryTooLong", "Discussion category must be 50 characters or less.");

    public static readonly Error Locked =
        Error.Validation("Discussion.Locked", "Discussion is locked and cannot receive new comments.");

    public static readonly Error AlreadyLocked =
        Error.Validation("Discussion.AlreadyLocked", "Discussion is already locked.");

    public static readonly Error NotLocked =
        Error.Validation("Discussion.NotLocked", "Discussion is not locked.");

    public static readonly Error InsufficientPermissions =
        Error.Forbidden("Discussion.InsufficientPermissions", "You don't have permission to perform this action on the discussion.");
}
