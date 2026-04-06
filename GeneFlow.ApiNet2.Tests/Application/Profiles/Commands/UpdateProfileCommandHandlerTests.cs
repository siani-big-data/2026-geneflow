using GeneFlow.ApiNet2.Application.Profiles.Commands.UpdateProfile;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Commands;

/// <summary>
/// Unit tests for UpdateProfileCommandHandler.
/// </summary>
public class UpdateProfileCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IProfileUnitOfWork _unitOfWork = Substitute.For<IProfileUnitOfWork>();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(
            _profileRepository,
            _unitOfWork);
    }

    private static Profile CreateTestProfile()
    {
        var profileId = new ProfileId(1);
        var userId = new UserId(1);
        var name = PersonName.Create("Original", "Name").Value;
        return Profile.Create(profileId, userId, name).Value;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateProfile()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfileCommand(
            "U00000001",
            "Jane",
            "Smith",
            "Updated bio",
            "New York",
            "Researcher",
            "MIT",
            "Biology",
            "Genomics");

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
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Smith");
        result.Value.Bio.Should().Be("Updated bio");
        result.Value.Location.Should().Be("New York");
        result.Value.ProfessionalRole.Should().Be("Researcher");
        result.Value.InstitutionName.Should().Be("MIT");
        result.Value.InstitutionDepartment.Should().Be("Biology");
        result.Value.ResearchField.Should().Be("Genomics");
    }

    [Fact]
    public async Task Handle_WithNullOptionalFields_ShouldUpdateProfile()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfileCommand(
            "U00000001",
            "John",
            null,
            null,
            null,
            null,
            null,
            null,
            null);

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
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().BeNull();
        result.Value.Bio.Should().BeNull();
    }

    #endregion

    #region Validation Failures

    [Theory]
    [InlineData("invalid")]
    [InlineData("P00000001")]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure(string userId)
    {
        // Arrange
        var command = new UpdateProfileCommand(userId, "John", null, null, null, null, null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProfileNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateProfileCommand(
            "U00000001",
            "John",
            null, null, null, null, null, null, null);

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
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithInvalidFirstName_ShouldReturnFailure(string firstName)
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfileCommand(
            "U00000001",
            firstName,
            null, null, null, null, null, null, null);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidResearchField_ShouldReturnFailure()
    {
        // Arrange
        var profile = CreateTestProfile();
        var command = new UpdateProfileCommand(
            "U00000001",
            "John",
            null, null, null, null, null, null,
            "InvalidResearchField");

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
        var command = new UpdateProfileCommand(
            "U00000001",
            "John",
            null, null, null, null, null, null, null);

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
