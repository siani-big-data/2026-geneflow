using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Domain errors for the Profile aggregate. Errors are split across partials by
/// subdomain to keep each file focused and within the project size guideline.
/// This file groups existence and personal-name errors. Other partials:
/// <see cref="ProfileErrors"/> in <c>ProfileErrors.Affiliation.cs</c>,
/// <c>ProfileErrors.Identifiers.cs</c> and <c>ProfileErrors.Photo.cs</c>.
/// </summary>
public static partial class ProfileErrors
{
    /// <summary>Profile was not found.</summary>
    public static Error NotFound => Error.NotFound(
        "Profile.NotFound",
        "The specified profile was not found.");

    /// <summary>Profile already exists for this user.</summary>
    public static Error AlreadyExists => Error.Conflict(
        "Profile.AlreadyExists",
        "A profile already exists for this user.");

    /// <summary>First name is required.</summary>
    public static Error FirstNameRequired => Error.Validation(
        "Profile.FirstNameRequired",
        "First name is required.");

    /// <summary>First name is too short.</summary>
    public static Error FirstNameTooShort(int minLength) => Error.Validation(
        "Profile.FirstNameTooShort",
        $"First name must be at least {minLength} characters.");

    /// <summary>First name is too long.</summary>
    public static Error FirstNameTooLong(int maxLength) => Error.Validation(
        "Profile.FirstNameTooLong",
        $"First name cannot exceed {maxLength} characters.");

    /// <summary>First name contains invalid characters.</summary>
    public static Error FirstNameInvalidFormat => Error.Validation(
        "Profile.FirstNameInvalidFormat",
        "First name can only contain letters, spaces, hyphens, and apostrophes.");

    /// <summary>Last name is too short.</summary>
    public static Error LastNameTooShort(int minLength) => Error.Validation(
        "Profile.LastNameTooShort",
        $"Last name must be at least {minLength} characters.");

    /// <summary>Last name is too long.</summary>
    public static Error LastNameTooLong(int maxLength) => Error.Validation(
        "Profile.LastNameTooLong",
        $"Last name cannot exceed {maxLength} characters.");

    /// <summary>Last name contains invalid characters.</summary>
    public static Error LastNameInvalidFormat => Error.Validation(
        "Profile.LastNameInvalidFormat",
        "Last name can only contain letters, spaces, hyphens, and apostrophes.");

    /// <summary>Invalid research field.</summary>
    public static Error InvalidResearchField => Error.Validation(
        "Profile.InvalidResearchField",
        "The specified research field is not valid.");
}
