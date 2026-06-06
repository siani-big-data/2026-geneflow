using GeneFlow.ApiNet2.Domain.Profiles.Entities;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Pinned-studies errors for the Profiles context.
/// </summary>
public static partial class ProfileErrors
{
    /// <summary>Too many pinned studies for a single user.</summary>
    public static readonly Error TooManyPinned = Error.Validation(
        "Profile.TooManyPinned",
        $"A user can pin at most {PinnedStudy.MaxPinnedPerUser} studies.");

    /// <summary>The pinned list contains duplicate study ids.</summary>
    public static readonly Error DuplicatePinned = Error.Validation(
        "Profile.DuplicatePinned",
        "The pinned list contains duplicate study ids.");

    /// <summary>The provided study id is not in a valid format.</summary>
    public static readonly Error InvalidStudyId = Error.Validation(
        "Profile.InvalidStudyId",
        "One or more study ids are not in a valid format.");

    /// <summary>The provided user id is not in a valid format.</summary>
    public static readonly Error InvalidUserId = Error.Validation(
        "Profile.InvalidUserId",
        "The provided user id is not in a valid format.");
}
