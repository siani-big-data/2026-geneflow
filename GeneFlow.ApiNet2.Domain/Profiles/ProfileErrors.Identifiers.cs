using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Errors related to research identifiers (ORCID) and external website URL.
/// Partial of <see cref="ProfileErrors"/>.
/// </summary>
public static partial class ProfileErrors
{
    /// <summary>ORCID ID has invalid format.</summary>
    public static Error OrcidIdInvalidFormat => Error.Validation(
        "Profile.OrcidIdInvalidFormat",
        "ORCID ID must be in the format 0000-0000-0000-0000 or 0000-0000-0000-000X.");

    /// <summary>Website URL is invalid.</summary>
    public static Error WebsiteInvalidFormat => Error.Validation(
        "Profile.WebsiteInvalidFormat",
        "Website must be a valid URL.");

    /// <summary>Website URL is too long.</summary>
    public static Error WebsiteTooLong(int maxLength) => Error.Validation(
        "Profile.WebsiteTooLong",
        $"Website URL cannot exceed {maxLength} characters.");
}
