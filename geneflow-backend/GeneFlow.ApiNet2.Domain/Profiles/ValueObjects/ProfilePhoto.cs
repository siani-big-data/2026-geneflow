using System.Text.RegularExpressions;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

/// <summary>
/// Represents profile photo information.
/// </summary>
public sealed partial class ProfilePhoto : ValueObject
{
    /// <summary>Maximum length of photo URL.</summary>
    public const int UrlMaxLength = 1000;

    /// <summary>Maximum photo size in bytes (10 MB).</summary>
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly Regex AbsoluteUrlRegex = GeneratedAbsoluteUrlRegex();
    private static readonly Regex RelativeUrlRegex = GeneratedRelativeUrlRegex();

    /// <summary>Gets the photo URL.</summary>
    public string? Url { get; }

    /// <summary>Gets the thumbnail URL.</summary>
    public string? ThumbnailUrl { get; }

    /// <summary>Gets the photo size in bytes.</summary>
    public long? SizeBytes { get; }

    /// <summary>Gets whether a photo is set.</summary>
    public bool HasPhoto => !string.IsNullOrWhiteSpace(Url);

    private ProfilePhoto(string? url, string? thumbnailUrl, long? sizeBytes)
    {
        Url = url;
        ThumbnailUrl = thumbnailUrl;
        SizeBytes = sizeBytes;
    }

    /// <summary>
    /// Creates validated ProfilePhoto.
    /// </summary>
    public static Result<ProfilePhoto> Create(string? url, string? thumbnailUrl = null, long? sizeBytes = null)
    {
        var trimmedUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        var trimmedThumbnail = string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl.Trim();

        // If no URL provided, return empty
        if (trimmedUrl is null)
            return Empty;

        // Validate URL
        if (trimmedUrl.Length > UrlMaxLength)
            return Result.Failure<ProfilePhoto>(ProfileErrors.PhotoUrlTooLong(UrlMaxLength));

        if (!IsValidUrl(trimmedUrl))
            return Result.Failure<ProfilePhoto>(ProfileErrors.PhotoUrlInvalidFormat);

        // Validate thumbnail URL
        if (trimmedThumbnail is not null)
        {
            if (trimmedThumbnail.Length > UrlMaxLength)
                return Result.Failure<ProfilePhoto>(ProfileErrors.PhotoUrlTooLong(UrlMaxLength));

            if (!IsValidUrl(trimmedThumbnail))
                return Result.Failure<ProfilePhoto>(ProfileErrors.PhotoUrlInvalidFormat);
        }

        // Validate size
        if (sizeBytes.HasValue && sizeBytes.Value > MaxSizeBytes)
            return Result.Failure<ProfilePhoto>(ProfileErrors.PhotoSizeExceedsLimit(MaxSizeBytes));

        return new ProfilePhoto(trimmedUrl, trimmedThumbnail, sizeBytes);
    }

    /// <summary>
    /// Creates empty ProfilePhoto.
    /// </summary>
    public static ProfilePhoto Empty => new(null, null, null);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Url;
        yield return ThumbnailUrl;
        yield return SizeBytes;
    }

    /// <inheritdoc />
    public override string ToString() => Url ?? string.Empty;

    /// <summary>
    /// Validates if a URL is valid (absolute or relative storage path).
    /// </summary>
    private static bool IsValidUrl(string url)
    {
        return AbsoluteUrlRegex.IsMatch(url) || RelativeUrlRegex.IsMatch(url);
    }

    // Absolute URL validation (https://...)
    [GeneratedRegex(@"^https?://[\w\-]+(\.[\w\-]+)+(/[\w\-._~:/?#\[\]@!$&'()*+,;=%]*)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex GeneratedAbsoluteUrlRegex();

    // Relative storage path validation (/storage/...)
    [GeneratedRegex(@"^/storage/[\w\-._~:/?#\[\]@!$&'()*+,;=%]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex GeneratedRelativeUrlRegex();
}
