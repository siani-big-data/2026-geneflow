using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Errors related to bio, location, professional role and institution
/// affiliation fields. Partial of <see cref="ProfileErrors"/>.
/// </summary>
public static partial class ProfileErrors
{
    /// <summary>Bio is too short.</summary>
    public static Error BioTooShort(int minLength) => Error.Validation(
        "Profile.BioTooShort",
        $"Bio must be at least {minLength} characters if provided.");

    /// <summary>Bio is too long.</summary>
    public static Error BioTooLong(int maxLength) => Error.Validation(
        "Profile.BioTooLong",
        $"Bio cannot exceed {maxLength} characters.");

    /// <summary>Location is too short.</summary>
    public static Error LocationTooShort(int minLength) => Error.Validation(
        "Profile.LocationTooShort",
        $"Location must be at least {minLength} characters if provided.");

    /// <summary>Location is too long.</summary>
    public static Error LocationTooLong(int maxLength) => Error.Validation(
        "Profile.LocationTooLong",
        $"Location cannot exceed {maxLength} characters.");

    /// <summary>Professional role is too short.</summary>
    public static Error ProfessionalRoleTooShort(int minLength) => Error.Validation(
        "Profile.ProfessionalRoleTooShort",
        $"Professional role must be at least {minLength} characters if provided.");

    /// <summary>Professional role is too long.</summary>
    public static Error ProfessionalRoleTooLong(int maxLength) => Error.Validation(
        "Profile.ProfessionalRoleTooLong",
        $"Professional role cannot exceed {maxLength} characters.");

    /// <summary>Institution name is too short.</summary>
    public static Error InstitutionNameTooShort(int minLength) => Error.Validation(
        "Profile.InstitutionNameTooShort",
        $"Institution name must be at least {minLength} characters if provided.");

    /// <summary>Institution name is too long.</summary>
    public static Error InstitutionNameTooLong(int maxLength) => Error.Validation(
        "Profile.InstitutionNameTooLong",
        $"Institution name cannot exceed {maxLength} characters.");

    /// <summary>Institution department is too short.</summary>
    public static Error InstitutionDepartmentTooShort(int minLength) => Error.Validation(
        "Profile.InstitutionDepartmentTooShort",
        $"Institution department must be at least {minLength} characters if provided.");

    /// <summary>Institution department is too long.</summary>
    public static Error InstitutionDepartmentTooLong(int maxLength) => Error.Validation(
        "Profile.InstitutionDepartmentTooLong",
        $"Institution department cannot exceed {maxLength} characters.");
}
