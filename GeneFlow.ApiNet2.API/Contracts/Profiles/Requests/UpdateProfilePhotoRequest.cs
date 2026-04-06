using System.ComponentModel.DataAnnotations;

namespace GeneFlow.ApiNet2.API.Contracts.Profiles.Requests;

/// <summary>
/// Request model for updating the profile photo.
/// </summary>
public sealed record UpdateProfilePhotoRequest
{
    /// <summary>The URL of the profile photo.</summary>
    [Required]
    [Url]
    [StringLength(1000)]
    public required string PhotoUrl { get; init; }

    /// <summary>The URL of the thumbnail image (optional).</summary>
    [Url]
    [StringLength(1000)]
    public string? ThumbnailUrl { get; init; }

    /// <summary>The size of the photo in bytes (optional, max 10MB).</summary>
    [Range(0, 10485760, ErrorMessage = "Photo size cannot exceed 10MB")]
    public long? SizeBytes { get; init; }
}
