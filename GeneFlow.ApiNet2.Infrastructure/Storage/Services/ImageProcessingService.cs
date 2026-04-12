using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace GeneFlow.ApiNet2.Infrastructure.Storage.Services;

/// <summary>
/// Image processing service using ImageSharp for thumbnails and resizing.
/// </summary>
public sealed class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;

    /// <summary>
    /// Initializes a new instance of the ImageProcessingService.
    /// </summary>
    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]> CreateThumbnailAsync(
        byte[] imageData,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        using var image = Image.Load(imageData);

        // Calculate aspect ratio preserving dimensions that fit within the target
        var ratioX = (double)width / image.Width;
        var ratioY = (double)height / image.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);

        // Resize the image
        image.Mutate(x => x
            .Resize(new ResizeOptions
            {
                Size = new Size(newWidth, newHeight),
                Mode = ResizeMode.Max
            }));

        // Export as JPEG for smaller file size
        using var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = 85 }, cancellationToken);

        _logger.LogDebug(
            "Created thumbnail {Width}x{Height} from {OriginalWidth}x{OriginalHeight}",
            newWidth, newHeight, image.Width, image.Height);

        return output.ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> ResizeImageAsync(
        byte[] imageData,
        int maxWidth,
        int maxHeight,
        CancellationToken cancellationToken = default)
    {
        using var image = Image.Load(imageData);

        // Only resize if larger than max dimensions
        if (image.Width <= maxWidth && image.Height <= maxHeight)
        {
            return imageData;
        }

        var ratioX = (double)maxWidth / image.Width;
        var ratioY = (double)maxHeight / image.Height;
        var ratio = Math.Min(ratioX, ratioY);

        var newWidth = (int)(image.Width * ratio);
        var newHeight = (int)(image.Height * ratio);

        image.Mutate(x => x
            .Resize(new ResizeOptions
            {
                Size = new Size(newWidth, newHeight),
                Mode = ResizeMode.Max
            }));

        using var output = new MemoryStream();

        // Preserve the original format with good quality
        var format = Image.DetectFormat(imageData);
        if (format?.DefaultMimeType == "image/png")
        {
            await image.SaveAsync(output, new PngEncoder(), cancellationToken);
        }
        else if (format?.DefaultMimeType == "image/webp")
        {
            await image.SaveAsync(output, new WebpEncoder { Quality = 90 }, cancellationToken);
        }
        else
        {
            await image.SaveAsync(output, new JpegEncoder { Quality = 90 }, cancellationToken);
        }

        _logger.LogDebug(
            "Resized image from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}",
            image.Width, image.Height, newWidth, newHeight);

        return output.ToArray();
    }
}
