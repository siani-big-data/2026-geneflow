using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Activity;

/// <summary>
/// Catalogue of <see cref="Error"/> values raised by the Activity bounded context.
/// </summary>
public static class ActivityErrors
{
    /// <summary>
    /// Returned when the request carries a user identifier that cannot be parsed.
    /// </summary>
    public static readonly Error InvalidUserId = Error.Validation(
        "Activity.InvalidUserId",
        "The supplied user identifier is malformed.");

    /// <summary>
    /// Returned when a client supplies a malformed pagination cursor.
    /// </summary>
    public static readonly Error InvalidCursor = Error.Validation(
        "Activity.InvalidCursor",
        "The supplied activity cursor is malformed.");

    /// <summary>
    /// Returned when a client requests a page size outside the supported range.
    /// </summary>
    public static Error InvalidLimit(int min, int max) => Error.Validation(
        "Activity.InvalidLimit",
        $"The activity page size must be between {min} and {max}.");

    /// <summary>
    /// Returned when an activity event cannot be located.
    /// </summary>
    public static Error NotFound(ActivityEventId id) => Error.NotFound(
        "Activity.NotFound",
        $"Activity event '{id}' was not found.");
}
