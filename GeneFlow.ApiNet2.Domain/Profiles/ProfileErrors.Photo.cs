using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles;

/// <summary>
/// Errors related to profile photo URL, format, size and content validation.
/// Partial of <see cref="ProfileErrors"/>.
/// </summary>
public static partial class ProfileErrors
{
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

    /// <summary>Photo format is not supported.</summary>
    public static Error InvalidPhotoFormat => Error.Validation(
        "Profile.InvalidPhotoFormat",
        "Photo format is not supported. Allowed formats: JPEG, PNG, GIF, WebP.");

    /// <summary>Photo is too large.</summary>
    public static Error PhotoTooLarge => Error.Validation(
        "Profile.PhotoTooLarge",
        "Photo size cannot exceed 10 MB.");

    /// <summary>Photo data is invalid or empty.</summary>
    public static Error InvalidPhotoData => Error.Validation(
        "Profile.InvalidPhotoData",
        "Photo data is invalid or empty.");
}
