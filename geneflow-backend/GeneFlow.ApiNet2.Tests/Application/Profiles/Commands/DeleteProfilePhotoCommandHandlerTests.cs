using GeneFlow.ApiNet2.Application.Profiles.Commands.DeleteProfilePhoto;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Commands;

/// <summary>
/// Unit tests for DeleteProfilePhotoCommandHandler.
/// </summary>
public class DeleteProfilePhotoCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IProfileUnitOfWork _unitOfWork = Substitute.For<IProfileUnitOfWork>();
    private readonly IPublisher _mediatorPublisher = Substitute.For<IPublisher>();
    private readonly ILogger<DeleteProfilePhotoCommandHandler> _logger = Substitute.For<ILogger<DeleteProfilePhotoCommandHandler>>();
    private readonly DeleteProfilePhotoCommandHandler _handler;

    public DeleteProfilePhotoCommandHandlerTests()
    {
        _handler = new DeleteProfilePhotoCommandHandler(
            _profileRepository,
            _unitOfWork,
            _mediatorPublisher,
            _logger);
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
    public async Task Handle_WithExistingPhoto_ShouldDeletePhoto()
    {
        // Arrange
        var profile = CreateTestProfile();
        var photo = ProfilePhoto.Create("https://example.com/photo.jpg").Value;
        profile.UpdatePhoto(photo);

        var command = new DeleteProfilePhotoCommand("U00000001");

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
        profile.Photo.Url.Should().BeNull();
        profile.Photo.HasPhoto.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNoPhoto_ShouldStillSucceed()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new DeleteProfilePhotoCommand("U00000001");

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
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteProfilePhotoCommand("invalid");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProfileNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteProfilePhotoCommand("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallUpdateOnRepository()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new DeleteProfilePhotoCommand("U00000001");

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

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var profile = CreateTestProfile();
        var photo = ProfilePhoto.Create("https://example.com/photo.jpg").Value;
        profile.UpdatePhoto(photo);

        var command = new DeleteProfilePhotoCommand("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPublishProfilePhotoDeletedEvent()
    {
        // Arrange
        var profile = CreateTestProfile();
        var photo = ProfilePhoto.Create("https://example.com/photo.jpg").Value;
        profile.UpdatePhoto(photo);

        var command = new DeleteProfilePhotoCommand("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mediatorPublisher.Received(1).Publish(
            Arg.Is<GeneFlow.ApiNet2.Domain.Profiles.Events.ProfilePhotoDeletedEvent>(e => e.ProfileId == profile.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ShouldNotCallSaveChanges()
    {
        // Arrange
        var command = new DeleteProfilePhotoCommand("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ShouldNotPublishEvent()
    {
        // Arrange
        var command = new DeleteProfilePhotoCommand("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mediatorPublisher.DidNotReceive().Publish(
            Arg.Any<GeneFlow.ApiNet2.Domain.Profiles.Events.ProfilePhotoDeletedEvent>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithValidUserId_ShouldDeletePhoto()
    {
        // Arrange
        var profile = CreateTestProfile();
        var photo = ProfilePhoto.Create("https://example.com/photo.jpg", "https://example.com/thumb.jpg", 1024).Value;
        profile.UpdatePhoto(photo);

        var command = new DeleteProfilePhotoCommand("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Verify photo exists before delete
        profile.Photo.HasPhoto.Should().BeTrue();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Photo.HasPhoto.Should().BeFalse();
        profile.Photo.Url.Should().BeNull();
        profile.Photo.ThumbnailUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentProfile_ShouldReturnNotFound()
    {
        // Arrange
        var command = new DeleteProfilePhotoCommand("U00000999");

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
    public async Task Handle_WhenNoPhoto_ShouldSucceed()
    {
        // Arrange
        var profile = CreateTestProfile();
        // Profile created without photo - Photo should be empty
        profile.Photo.HasPhoto.Should().BeFalse();

        var command = new DeleteProfilePhotoCommand("U00000001");

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
        profile.Photo.HasPhoto.Should().BeFalse();
    }

    #endregion
}
