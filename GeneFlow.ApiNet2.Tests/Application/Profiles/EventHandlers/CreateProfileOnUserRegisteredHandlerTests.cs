using GeneFlow.ApiNet2.Application.Profiles.EventHandlers;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.EventHandlers;

/// <summary>
/// Unit tests for CreateProfileOnUserRegisteredHandler.
/// </summary>
public class CreateProfileOnUserRegisteredHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IProfileUnitOfWork _unitOfWork = Substitute.For<IProfileUnitOfWork>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly ILogger<CreateProfileOnUserRegisteredHandler> _logger =
        Substitute.For<ILogger<CreateProfileOnUserRegisteredHandler>>();
    private readonly CreateProfileOnUserRegisteredHandler _handler;

    public CreateProfileOnUserRegisteredHandlerTests()
    {
        _handler = new CreateProfileOnUserRegisteredHandler(
            _profileRepository,
            _unitOfWork,
            _sequenceGenerator,
            _logger);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithUserRegisteredEvent_ShouldCreateProfile()
    {
        // Arrange
        var userId = new UserId(1);
        var @event = new UserRegisteredEvent(userId, "test@example.com", "johndoe", "verification-token");

        _sequenceGenerator
            .NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        _profileRepository
            .ExistsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _profileRepository.Received(1).AddAsync(
            Arg.Is<Profile>(p => p.Name.FirstName == "johndoe"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUseUsernameAsFirstName()
    {
        // Arrange
        var userId = new UserId(1);
        // Note: Username must be valid for PersonName (only letters, spaces, hyphens, apostrophes)
        var @event = new UserRegisteredEvent(userId, "alice@example.com", "AliceResearcher", "verification-token");

        _sequenceGenerator
            .NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        _profileRepository
            .ExistsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _profileRepository.Received(1).AddAsync(
            Arg.Is<Profile>(p => p.Name.FirstName == "AliceResearcher"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Skip Cases

    [Fact]
    public async Task Handle_WhenProfileAlreadyExists_ShouldNotCreateAnother()
    {
        // Arrange
        var userId = new UserId(1);
        var @event = new UserRegisteredEvent(userId, "test@example.com", "johndoe", "verification-token");

        _profileRepository
            .ExistsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _profileRepository.DidNotReceive().AddAsync(Arg.Any<Profile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldGenerateNewProfileId()
    {
        // Arrange
        var userId = new UserId(1);
        var @event = new UserRegisteredEvent(userId, "test@example.com", "johndoe", "verification-token");

        _sequenceGenerator
            .NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(42L);

        _profileRepository
            .ExistsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>());
        await _profileRepository.Received(1).AddAsync(
            Arg.Is<Profile>(p => p.Id.ToString() == "P00000042"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCheckIfProfileExists()
    {
        // Arrange
        var userId = new UserId(1);
        var @event = new UserRegisteredEvent(userId, "test@example.com", "johndoe", "verification-token");

        _profileRepository
            .ExistsForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        await _profileRepository.Received(1).ExistsForUserAsync(userId, Arg.Any<CancellationToken>());
    }

    #endregion
}
