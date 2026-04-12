namespace GeneFlow.ApiNet2.SharedKernel.Infrastructure;

/// <summary>
/// Service for processing images (resizing, thumbnails, etc.).
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Creates a thumbnail from an image.
    /// </summary>
    /// <param name="imageData">The original image data.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The thumbnail image data.</returns>
    Task<byte[]> CreateThumbnailAsync(
        byte[] imageData,
        int width,
        int height,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resizes an image to fit within the specified dimensions while maintaining aspect ratio.
    /// </summary>
    /// <param name="imageData">The original image data.</param>
    /// <param name="maxWidth">Maximum width in pixels.</param>
    /// <param name="maxHeight">Maximum height in pixels.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resized image data.</returns>
    Task<byte[]> ResizeImageAsync(
        byte[] imageData,
        int maxWidth,
        int maxHeight,
        CancellationToken cancellationToken = default);
}
