using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Usage;

/// <summary>
/// Error definitions for the Usage domain.
/// </summary>
public static class UsageErrors
{
    public static readonly Error UserNotFound = Error.NotFound(
        "Usage.UserNotFound",
        "User not found");

    public static readonly Error StatsNotFound = Error.NotFound(
        "Usage.StatsNotFound",
        "Usage statistics not found for this user");
}
