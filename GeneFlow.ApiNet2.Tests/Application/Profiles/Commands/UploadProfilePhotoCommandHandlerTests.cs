using GeneFlow.ApiNet2.Application.Profiles.Commands.UploadProfilePhoto;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Events;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Commands;

/// <summary>
/// Unit tests for UploadProfilePhotoCommandHandler.
/// </summary>
public class UploadProfilePhotoCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IProfileUnitOfWork _unitOfWork = Substitute.For<IProfileUnitOfWork>();
    private readonly IPublisher _mediatorPublisher = Substitute.For<IPublisher>();
    private readonly ILogger<UploadProfilePhotoCommandHandler> _logger = Substitute.For<ILogger<UploadProfilePhotoCommandHandler>>();
    private readonly UploadProfilePhotoCommandHandler _handler;

    // Valid base64 encoded 1x1 PNG (minimal valid image)
    private const string ValidBase64Image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    public UploadProfilePhotoCommandHandlerTests()
    {
        _handler = new UploadProfilePhotoCommandHandler(
            _profileRepository,
            _unitOfWork,
            _mediatorPublisher,
            _logger);
    }

    private static Profile CreateTestProfile(long id = 1)
    {
        var profileId = new ProfileId(id);
        var userId = new UserId(id);
        var name = PersonName.Create("John", "Doe").Value;
        return Profile.Create(profileId, userId, name).Value;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidImage_ShouldUploadPhoto()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.jpg",
            "image/jpeg",
            1024);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PhotoUrl.Should().NotBeNullOrWhiteSpace();
        result.Value.PhotoThumbnailUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_WithPngImage_ShouldUploadPhoto()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.png",
            "image/png",
            2048);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PhotoUrl.Should().Contain(".png");
    }

    [Theory]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/jpg", "jpg")]
    [InlineData("image/png", "png")]
    [InlineData("image/gif", "gif")]
    [InlineData("image/webp", "webp")]
    public async Task Handle_WithValidFormats_ShouldUploadPhoto(string contentType, string expectedExtension)
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            $"photo.{expectedExtension}",
            contentType,
            1024);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PhotoUrl.Should().Contain($".{expectedExtension}");
    }

    [Fact]
    public async Task Handle_ShouldUpdateProfile()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.jpg",
            "image/jpeg",
            1024);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _profileRepository.Received(1).Update(profile);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPublishPhotoUploadedEvent()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.jpg",
            "image/jpeg",
            1024);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _mediatorPublisher.Received(1).Publish(
            Arg.Is<ProfilePhotoUploadedEvent>(e =>
                e.ProfileId == profile.Id &&
                e.PhotoData == ValidBase64Image &&
                e.Extension == "jpg" &&
                e.ContentType == "image/jpeg" &&
                e.SizeBytes == 1024),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMaxAllowedSize_ShouldSucceed()
    {
        // Arrange
        var profile = CreateTestProfile();
        var maxSize = 10 * 1024 * 1024; // 10 MB - exactly the max
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.jpg",
            "image/jpeg",
            maxSize);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnError()
    {
        // Arrange
        var command = new UploadProfilePhotoCommand(
            "invalid",
            ValidBase64Image,
            "photo.jpg",
            "image/jpeg",
            1024);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProfileNotFound_ShouldReturnError()
    {
        // Arrange
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.jpg",
            "image/jpeg",
            1024);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithOversizedImage_ShouldReturnError()
    {
        // Arrange
        var oversizedBytes = 11 * 1024 * 1024; // 11 MB - exceeds 10 MB limit
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.jpg",
            "image/jpeg",
            oversizedBytes);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TooLarge");
    }

    [Theory]
    [InlineData("image/bmp")]
    [InlineData("image/tiff")]
    [InlineData("image/svg+xml")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    public async Task Handle_WithInvalidFormat_ShouldReturnError(string contentType)
    {
        // Arrange
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.bmp",
            contentType,
            1024);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPhotoFormat");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WithEmptyBase64Data_ShouldReturnError(string? photoData)
    {
        // Arrange
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            photoData!,
            "photo.jpg",
            "image/jpeg",
            1024);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPhotoData");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallFileStorageUpload()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.jpg",
            "image/jpeg",
            1024);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - The handler publishes an event that triggers file storage
        await _mediatorPublisher.Received(1).Publish(
            Arg.Is<ProfilePhotoUploadedEvent>(e => e.ProfileId == profile.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldGenerateCorrectStorageUrls()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.png",
            "image/png",
            1024);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PhotoUrl.Should().Contain($"/storage/profiles/{profile.Id.Value}/photo.png");
        result.Value.PhotoThumbnailUrl.Should().Contain($"/storage/profiles/{profile.Id.Value}/thumbnail.png");
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldNotCallRepository()
    {
        // Arrange
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.bmp",
            "image/bmp", // Invalid format
            1024);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _profileRepository.DidNotReceive().GetByUserIdAsync(
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldNotPublishEvent()
    {
        // Arrange
        var command = new UploadProfilePhotoCommand(
            "U00000001",
            ValidBase64Image,
            "photo.jpg",
            "image/bmp", // Invalid format
            1024);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mediatorPublisher.DidNotReceive().Publish(
            Arg.Any<ProfilePhotoUploadedEvent>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
