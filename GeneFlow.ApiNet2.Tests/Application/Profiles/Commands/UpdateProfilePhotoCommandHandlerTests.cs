using GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateProfilePhoto;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Commands;

/// <summary>
/// Unit tests for UpdateProfilePhotoCommandHandler.
/// </summary>
public class UpdateProfilePhotoCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IProfileUnitOfWork _unitOfWork = Substitute.For<IProfileUnitOfWork>();
    private readonly UpdateProfilePhotoCommandHandler _handler;

    public UpdateProfilePhotoCommandHandlerTests()
    {
        _handler = new UpdateProfilePhotoCommandHandler(
            _profileRepository,
            _unitOfWork);
    }

    private static Profile CreateTestProfile()
    {
        var profileId = new ProfileId(1);
        var userId = new UserId(1);
        var name = PersonName.Create("John", "Doe").Value;
        return Profile.Create(profileId, userId, name).Value;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidPhoto_ShouldUpdate()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfilePhotoCommand(
            "U00000001",
            "https://example.com/photo.jpg",
            "https://example.com/thumb.jpg",
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
        result.Value.PhotoUrl.Should().Be("https://example.com/photo.jpg");
        result.Value.PhotoThumbnailUrl.Should().Be("https://example.com/thumb.jpg");
    }

    [Fact]
    public async Task Handle_WithOnlyPhotoUrl_ShouldUpdate()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfilePhotoCommand(
            "U00000001",
            "https://example.com/photo.jpg");

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
        result.Value.PhotoUrl.Should().Be("https://example.com/photo.jpg");
        result.Value.PhotoThumbnailUrl.Should().BeNull();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateProfilePhotoCommand("invalid", "https://example.com/photo.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProfileNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateProfilePhotoCommand("U00000001", "https://example.com/photo.jpg");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid.com/photo.jpg")]
    public async Task Handle_WithInvalidPhotoUrl_ShouldReturnFailure(string photoUrl)
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfilePhotoCommand("U00000001", photoUrl);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithPhotoTooLarge_ShouldReturnFailure()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfilePhotoCommand(
            "U00000001",
            "https://example.com/photo.jpg",
            null,
            11 * 1024 * 1024); // 11MB, exceeds 10MB limit

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallUpdateOnRepository()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfilePhotoCommand(
            "U00000001",
            "https://example.com/photo.jpg");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _profileRepository.Received(1).Update(Arg.Any<Profile>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
