using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.ValueObjects;

/// <summary>
/// Unit tests for the ProfilePhoto value object.
/// </summary>
public class ProfilePhotoTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidAbsoluteUrl_ShouldReturnSuccess()
    {
        // Arrange
        var url = "https://example.com/photos/profile.jpg";

        // Act
        var result = ProfilePhoto.Create(url);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be(url);
        result.Value.HasPhoto.Should().BeTrue();
    }

    [Fact]
    public void Create_WithValidHttpsUrl_ShouldReturnSuccess()
    {
        // Arrange
        var url = "https://cdn.example.com/images/user-photo.png";

        // Act
        var result = ProfilePhoto.Create(url);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be(url);
    }

    [Fact]
    public void Create_WithValidHttpUrl_ShouldReturnSuccess()
    {
        // Arrange
        var url = "http://example.com/photo.jpg";

        // Act
        var result = ProfilePhoto.Create(url);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be(url);
    }

    [Fact]
    public void Create_WithValidRelativeStoragePath_ShouldReturnSuccess()
    {
        // Arrange
        var url = "/storage/profiles/user-123/photo.jpg";

        // Act
        var result = ProfilePhoto.Create(url);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be(url);
    }

    [Fact]
    public void Create_WithUrlAndThumbnail_ShouldReturnSuccess()
    {
        // Arrange
        var url = "https://example.com/photos/profile.jpg";
        var thumbnailUrl = "https://example.com/photos/profile-thumb.jpg";

        // Act
        var result = ProfilePhoto.Create(url, thumbnailUrl);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be(url);
        result.Value.ThumbnailUrl.Should().Be(thumbnailUrl);
    }

    [Fact]
    public void Create_WithUrlThumbnailAndSize_ShouldReturnSuccess()
    {
        // Arrange
        var url = "https://example.com/photos/profile.jpg";
        var thumbnailUrl = "https://example.com/photos/profile-thumb.jpg";
        var sizeBytes = 1024L * 500; // 500 KB

        // Act
        var result = ProfilePhoto.Create(url, thumbnailUrl, sizeBytes);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be(url);
        result.Value.ThumbnailUrl.Should().Be(thumbnailUrl);
        result.Value.SizeBytes.Should().Be(sizeBytes);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromUrl()
    {
        // Arrange
        var url = "  https://example.com/photo.jpg  ";

        // Act
        var result = ProfilePhoto.Create(url);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromThumbnailUrl()
    {
        // Arrange
        var url = "https://example.com/photo.jpg";
        var thumbnailUrl = "  https://example.com/thumb.jpg  ";

        // Act
        var result = ProfilePhoto.Create(url, thumbnailUrl);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ThumbnailUrl.Should().Be("https://example.com/thumb.jpg");
    }

    #endregion

    #region Create - Null/Empty Handling

    [Fact]
    public void Create_WithNullUrl_ShouldReturnEmpty()
    {
        // Act
        var result = ProfilePhoto.Create(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(ProfilePhoto.Empty);
        result.Value.HasPhoto.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyUrl_ShouldReturnEmpty()
    {
        // Act
        var result = ProfilePhoto.Create(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(ProfilePhoto.Empty);
        result.Value.HasPhoto.Should().BeFalse();
    }

    [Fact]
    public void Create_WithWhitespaceUrl_ShouldReturnEmpty()
    {
        // Act
        var result = ProfilePhoto.Create("   ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(ProfilePhoto.Empty);
        result.Value.HasPhoto.Should().BeFalse();
    }

    [Fact]
    public void Create_WithWhitespaceThumbnailUrl_ShouldTreatAsNull()
    {
        // Arrange
        var url = "https://example.com/photo.jpg";

        // Act
        var result = ProfilePhoto.Create(url, "   ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ThumbnailUrl.Should().BeNull();
    }

    #endregion

    #region Create - URL Validation Failures

    [Fact]
    public void Create_WithUrlTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longPath = new string('a', ProfilePhoto.UrlMaxLength);
        var url = $"https://example.com/{longPath}";

        // Act
        var result = ProfilePhoto.Create(url);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PhotoUrlTooLong");
    }

    [Fact]
    public void Create_WithThumbnailUrlTooLong_ShouldReturnFailure()
    {
        // Arrange
        var url = "https://example.com/photo.jpg";
        var longPath = new string('a', ProfilePhoto.UrlMaxLength);
        var thumbnailUrl = $"https://example.com/{longPath}";

        // Act
        var result = ProfilePhoto.Create(url, thumbnailUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PhotoUrlTooLong");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/photo.jpg")]
    [InlineData("file:///c:/photo.jpg")]
    [InlineData("//example.com/photo.jpg")]
    [InlineData("example.com/photo.jpg")]
    public void Create_WithInvalidUrlFormat_ShouldReturnFailure(string invalidUrl)
    {
        // Act
        var result = ProfilePhoto.Create(invalidUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PhotoUrlInvalidFormat");
    }

    [Fact]
    public void Create_WithInvalidThumbnailUrlFormat_ShouldReturnFailure()
    {
        // Arrange
        var url = "https://example.com/photo.jpg";
        var invalidThumbnailUrl = "not-a-valid-url";

        // Act
        var result = ProfilePhoto.Create(url, invalidThumbnailUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PhotoUrlInvalidFormat");
    }

    [Fact]
    public void Create_WithInvalidRelativePath_ShouldReturnFailure()
    {
        // Arrange - relative path that doesn't start with /storage/
        var invalidPath = "/images/photo.jpg";

        // Act
        var result = ProfilePhoto.Create(invalidPath);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PhotoUrlInvalidFormat");
    }

    #endregion

    #region Create - Size Validation

    [Fact]
    public void Create_WithSizeExceedingLimit_ShouldReturnFailure()
    {
        // Arrange
        var url = "https://example.com/photo.jpg";
        var sizeBytes = ProfilePhoto.MaxSizeBytes + 1;

        // Act
        var result = ProfilePhoto.Create(url, null, sizeBytes);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PhotoSizeExceedsLimit");
    }

    [Fact]
    public void Create_WithSizeAtLimit_ShouldReturnSuccess()
    {
        // Arrange
        var url = "https://example.com/photo.jpg";
        var sizeBytes = ProfilePhoto.MaxSizeBytes;

        // Act
        var result = ProfilePhoto.Create(url, null, sizeBytes);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SizeBytes.Should().Be(sizeBytes);
    }

    [Fact]
    public void Create_WithNullSize_ShouldReturnSuccess()
    {
        // Arrange
        var url = "https://example.com/photo.jpg";

        // Act
        var result = ProfilePhoto.Create(url, null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SizeBytes.Should().BeNull();
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldHaveNullProperties()
    {
        // Act
        var empty = ProfilePhoto.Empty;

        // Assert
        empty.Url.Should().BeNull();
        empty.ThumbnailUrl.Should().BeNull();
        empty.SizeBytes.Should().BeNull();
        empty.HasPhoto.Should().BeFalse();
    }

    [Fact]
    public void Empty_ShouldBeEqual()
    {
        // Act
        var empty1 = ProfilePhoto.Empty;
        var empty2 = ProfilePhoto.Empty;

        // Assert
        empty1.Should().Be(empty2);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var photo1 = ProfilePhoto.Create("https://example.com/photo.jpg", "https://example.com/thumb.jpg", 1000).Value;
        var photo2 = ProfilePhoto.Create("https://example.com/photo.jpg", "https://example.com/thumb.jpg", 1000).Value;

        // Assert
        photo1.Should().Be(photo2);
    }

    [Fact]
    public void Equals_WithDifferentUrl_ShouldReturnFalse()
    {
        // Arrange
        var photo1 = ProfilePhoto.Create("https://example.com/photo1.jpg").Value;
        var photo2 = ProfilePhoto.Create("https://example.com/photo2.jpg").Value;

        // Assert
        photo1.Should().NotBe(photo2);
    }

    [Fact]
    public void Equals_WithDifferentThumbnailUrl_ShouldReturnFalse()
    {
        // Arrange
        var photo1 = ProfilePhoto.Create("https://example.com/photo.jpg", "https://example.com/thumb1.jpg").Value;
        var photo2 = ProfilePhoto.Create("https://example.com/photo.jpg", "https://example.com/thumb2.jpg").Value;

        // Assert
        photo1.Should().NotBe(photo2);
    }

    [Fact]
    public void Equals_WithDifferentSize_ShouldReturnFalse()
    {
        // Arrange
        var photo1 = ProfilePhoto.Create("https://example.com/photo.jpg", null, 1000).Value;
        var photo2 = ProfilePhoto.Create("https://example.com/photo.jpg", null, 2000).Value;

        // Assert
        photo1.Should().NotBe(photo2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WithUrl_ShouldReturnUrl()
    {
        // Arrange
        var url = "https://example.com/photo.jpg";
        var photo = ProfilePhoto.Create(url).Value;

        // Act
        var result = photo.ToString();

        // Assert
        result.Should().Be(url);
    }

    [Fact]
    public void ToString_WithEmpty_ShouldReturnEmptyString()
    {
        // Act
        var result = ProfilePhoto.Empty.ToString();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Constants

    [Fact]
    public void UrlMaxLength_ShouldBe1000()
    {
        ProfilePhoto.UrlMaxLength.Should().Be(1000);
    }

    [Fact]
    public void MaxSizeBytes_ShouldBe10MB()
    {
        ProfilePhoto.MaxSizeBytes.Should().Be(10 * 1024 * 1024);
    }

    #endregion
}
