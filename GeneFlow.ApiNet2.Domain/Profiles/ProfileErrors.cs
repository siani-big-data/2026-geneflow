using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Domain errors for Profile aggregate.
/// </summary>
public static class ProfileErrors
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

    /// <summary>Last name is too long.</summary>
    public static Error LastNameTooLong(int maxLength) => Error.Validation(
        "Profile.LastNameTooLong",
        $"Last name cannot exceed {maxLength} characters.");

    /// <summary>Last name contains invalid characters.</summary>
    public static Error LastNameInvalidFormat => Error.Validation(
        "Profile.LastNameInvalidFormat",
        "Last name can only contain letters, spaces, hyphens, and apostrophes.");

    /// <summary>Bio is too long.</summary>
    public static Error BioTooLong(int maxLength) => Error.Validation(
        "Profile.BioTooLong",
        $"Bio cannot exceed {maxLength} characters.");

    /// <summary>Location is too long.</summary>
    public static Error LocationTooLong(int maxLength) => Error.Validation(
        "Profile.LocationTooLong",
        $"Location cannot exceed {maxLength} characters.");

    /// <summary>Professional role is too long.</summary>
    public static Error ProfessionalRoleTooLong(int maxLength) => Error.Validation(
        "Profile.ProfessionalRoleTooLong",
        $"Professional role cannot exceed {maxLength} characters.");

    /// <summary>Institution name is too long.</summary>
    public static Error InstitutionNameTooLong(int maxLength) => Error.Validation(
        "Profile.InstitutionNameTooLong",
        $"Institution name cannot exceed {maxLength} characters.");

    /// <summary>Institution department is too long.</summary>
    public static Error InstitutionDepartmentTooLong(int maxLength) => Error.Validation(
        "Profile.InstitutionDepartmentTooLong",
        $"Institution department cannot exceed {maxLength} characters.");

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

    /// <summary>Photo URL is invalid.</summary>
    public static Error PhotoUrlInvalidFormat => Error.Validation(
        "Profile.PhotoUrlInvalidFormat",
        "Photo URL must be a valid URL.");

    /// <summary>Photo URL is too long.</summary>
    public static Error PhotoUrlTooLong(int maxLength) => Error.Validation(
        "Profile.PhotoUrlTooLong",
        $"Photo URL cannot exceed {maxLength} characters.");

    /// <summary>Photo size exceeds the maximum allowed.</summary>
    public static Error PhotoSizeExceedsLimit(long maxSizeBytes) => Error.Validation(
        "Profile.PhotoSizeExceedsLimit",
        $"Photo size cannot exceed {maxSizeBytes / 1024 / 1024} MB.");

    /// <summary>Invalid research field.</summary>
    public static Error InvalidResearchField => Error.Validation(
        "Profile.InvalidResearchField",
        "The specified research field is not valid.");
}
