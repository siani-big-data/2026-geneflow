using GeneFlow.ApiNet2.Infrastructure.Storage.Services;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for the ImageProcessingService.
/// Tests image resizing and thumbnail generation using real ImageSharp processing.
/// </summary>
public class ImageProcessingServiceTests
{
    private readonly ILogger<ImageProcessingService> _loggerMock;
    private readonly ImageProcessingService _service;

    public ImageProcessingServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<ImageProcessingService>>();
        _service = new ImageProcessingService(_loggerMock);
    }

    #region Helper Methods

    private static byte[] CreateTestImage(int width, int height, string format = "png")
    {
        using var image = new Image<Rgba32>(width, height);

        // Fill with a solid color
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32(100, 150, 200, 255);
            }
        }

        using var ms = new MemoryStream();
        if (format == "jpeg" || format == "jpg")
        {
            image.Save(ms, new JpegEncoder());
        }
        else
        {
            image.Save(ms, new PngEncoder());
        }

        return ms.ToArray();
    }

    private static (int Width, int Height) GetImageDimensions(byte[] imageData)
    {
        using var image = Image.Load(imageData);
        return (image.Width, image.Height);
    }

    #endregion

    #region CreateThumbnailAsync

    [Fact]
    public async Task CreateThumbnailAsync_ShouldReduceImageSize()
    {
        // Arrange
        var originalImage = CreateTestImage(800, 600);
        var targetWidth = 150;
        var targetHeight = 150;

        // Act
        var thumbnail = await _service.CreateThumbnailAsync(originalImage, targetWidth, targetHeight);

        // Assert
        thumbnail.Should().NotBeNull();
        thumbnail.Length.Should().BeLessThan(originalImage.Length);
    }

    [Fact]
    public async Task CreateThumbnailAsync_ShouldMaintainAspectRatio()
    {
        // Arrange
        var originalImage = CreateTestImage(800, 400); // 2:1 aspect ratio
        var targetWidth = 200;
        var targetHeight = 200;

        // Act
        var thumbnail = await _service.CreateThumbnailAsync(originalImage, targetWidth, targetHeight);
        var (width, height) = GetImageDimensions(thumbnail);

        // Assert - Should fit within 200x200 but maintain 2:1 ratio
        width.Should().BeLessOrEqualTo(targetWidth);
        height.Should().BeLessOrEqualTo(targetHeight);
        // Width should be 200, height should be ~100 to maintain ratio
        ((double)width / height).Should().BeApproximately(2.0, 0.1);
    }

    [Fact]
    public async Task CreateThumbnailAsync_ShouldOutputJpeg()
    {
        // Arrange
        var originalImage = CreateTestImage(400, 400);

        // Act
        var thumbnail = await _service.CreateThumbnailAsync(originalImage, 100, 100);

        // Assert - JPEG starts with FFD8
        thumbnail[0].Should().Be(0xFF);
        thumbnail[1].Should().Be(0xD8);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(50, 75)]
    [InlineData(200, 150)]
    public async Task CreateThumbnailAsync_ShouldRespectTargetDimensions(int targetWidth, int targetHeight)
    {
        // Arrange
        var originalImage = CreateTestImage(500, 500);

        // Act
        var thumbnail = await _service.CreateThumbnailAsync(originalImage, targetWidth, targetHeight);
        var (width, height) = GetImageDimensions(thumbnail);

        // Assert
        width.Should().BeLessOrEqualTo(targetWidth);
        height.Should().BeLessOrEqualTo(targetHeight);
    }

    #endregion

    #region ResizeImageAsync

    [Fact]
    public async Task ResizeImageAsync_WhenSmallerThanMax_ShouldReturnOriginal()
    {
        // Arrange
        var originalImage = CreateTestImage(400, 300);
        var maxWidth = 800;
        var maxHeight = 600;

        // Act
        var resized = await _service.ResizeImageAsync(originalImage, maxWidth, maxHeight);

        // Assert - Should return original data since image is smaller than max
        resized.Should().BeEquivalentTo(originalImage);
    }

    [Fact]
    public async Task ResizeImageAsync_WhenLargerThanMax_ShouldResize()
    {
        // Arrange
        var originalImage = CreateTestImage(1600, 1200);
        var maxWidth = 800;
        var maxHeight = 600;

        // Act
        var resized = await _service.ResizeImageAsync(originalImage, maxWidth, maxHeight);
        var (width, height) = GetImageDimensions(resized);

        // Assert
        width.Should().BeLessOrEqualTo(maxWidth);
        height.Should().BeLessOrEqualTo(maxHeight);
    }

    [Fact]
    public async Task ResizeImageAsync_ShouldMaintainAspectRatio()
    {
        // Arrange
        var originalImage = CreateTestImage(1600, 800); // 2:1 ratio
        var maxWidth = 400;
        var maxHeight = 400;

        // Act
        var resized = await _service.ResizeImageAsync(originalImage, maxWidth, maxHeight);
        var (width, height) = GetImageDimensions(resized);

        // Assert
        ((double)width / height).Should().BeApproximately(2.0, 0.1);
    }

    #endregion
}
