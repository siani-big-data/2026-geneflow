using GeneFlow.ApiNet2.Application.Profiles.Commands.CreateProfile;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Commands;

/// <summary>
/// Unit tests for CreateProfileCommandHandler.
/// </summary>
public class CreateProfileCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IProfileUnitOfWork _unitOfWork = Substitute.For<IProfileUnitOfWork>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly CreateProfileCommandHandler _handler;

    public CreateProfileCommandHandlerTests()
    {
        _handler = new CreateProfileCommandHandler(
            _profileRepository,
            _unitOfWork,
            _sequenceGenerator);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateProfile()
    {
        // Arrange
        var command = new CreateProfileCommand("U00000001", "John", "Doe");

        _sequenceGenerator
            .NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        _profileRepository
            .ExistsForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.UserId.Should().Be("U00000001");

        await _profileRepository.Received(1).AddAsync(Arg.Any<Profile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnProfileDto()
    {
        // Arrange
        var command = new CreateProfileCommand("U00000042", "Alice", null);

        _sequenceGenerator
            .NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(42L);

        _profileRepository
            .ExistsForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("P00000042");
        result.Value.FirstName.Should().Be("Alice");
        result.Value.LastName.Should().BeNull();
        result.Value.FullName.Should().Be("Alice");
        result.Value.Initials.Should().Be("A");
    }

    #endregion

    #region Validation Failures

    [Theory]
    [InlineData("invalid-user-id")]
    [InlineData("P00000001")] // Wrong prefix
    [InlineData("")]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure(string userId)
    {
        // Arrange
        var command = new CreateProfileCommand(userId, "John", "Doe");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithInvalidFirstName_ShouldReturnFailure(string firstName)
    {
        // Arrange
        var command = new CreateProfileCommand("U00000001", firstName, null);

        _profileRepository
            .ExistsForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Conflict Cases

    [Fact]
    public async Task Handle_WithExistingProfile_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateProfileCommand("U00000001", "John", "Doe");

        _profileRepository
            .ExistsForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyExists");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldGenerateSequenceId()
    {
        // Arrange
        var command = new CreateProfileCommand("U00000001", "John", "Doe");

        _sequenceGenerator
            .NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        _profileRepository
            .ExistsForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCheckForExistingProfile()
    {
        // Arrange
        var command = new CreateProfileCommand("U00000001", "John", "Doe");

        _profileRepository
            .ExistsForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _sequenceGenerator
            .NextAsync(ProfileId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _profileRepository.Received(1).ExistsForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
