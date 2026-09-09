using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// Follow-graph errors for the Identity context.
/// </summary>
public static partial class UserErrors
{
    /// <summary>A user cannot follow themselves.</summary>
    public static readonly Error CannotFollowSelf = Error.Validation(
        "User.CannotFollowSelf", "A user cannot follow themselves.");

    /// <summary>Already following the target user.</summary>
    public static readonly Error AlreadyFollowing = Error.Conflict(
        "User.AlreadyFollowing", "You are already following this user.");

    /// <summary>Not currently following the target user.</summary>
    public static readonly Error NotFollowing = Error.NotFound(
        "User.NotFollowing", "You are not following this user.");

    /// <summary>Invalid user id (could not be parsed).</summary>
    public static readonly Error InvalidUserId = Error.Validation(
        "User.InvalidUserId", "The provided user id is not in a valid format.");
}
