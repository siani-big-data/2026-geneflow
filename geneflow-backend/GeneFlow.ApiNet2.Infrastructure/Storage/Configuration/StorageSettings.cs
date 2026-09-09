namespace GeneFlow.ApiNet2.Infrastructure.Storage.Configuration;

/// <summary>
/// Configuration settings for file storage (Datalake).
/// </summary>
public sealed class StorageSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// Base path for local file storage.
    /// Default: "./uploads"
    /// </summary>
    public string BasePath { get; set; } = "./uploads";

    /// <summary>
    /// Base URL for serving stored files.
    /// Default: "/storage"
    /// </summary>
    public string BaseUrl { get; set; } = "/storage";

    /// <summary>
    /// Maximum file size in bytes.
    /// Default: 50 MB
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Enable thumbnail generation for images.
    /// Default: true
    /// </summary>
    public bool EnableThumbnails { get; set; } = true;

    /// <summary>
    /// Thumbnail width in pixels.
    /// Default: 150
    /// </summary>
    public int ThumbnailWidth { get; set; } = 150;

    /// <summary>
    /// Thumbnail height in pixels.
    /// Default: 150
    /// </summary>
    public int ThumbnailHeight { get; set; } = 150;
}
