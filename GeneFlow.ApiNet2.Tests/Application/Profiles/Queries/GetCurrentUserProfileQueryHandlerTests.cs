using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Profiles.Queries.GetCurrentUserProfile;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Queries;

/// <summary>
/// Unit tests for GetCurrentUserProfileQueryHandler.
/// </summary>
public class GetCurrentUserProfileQueryHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetCurrentUserProfileQueryHandler _handler;

    public GetCurrentUserProfileQueryHandlerTests()
    {
        _handler = new GetCurrentUserProfileQueryHandler(
            _profileRepository,
            _currentUserService);
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
    public async Task Handle_WithAuthenticatedUser_ShouldReturnProfile()
    {
        // Arrange
        var userId = new UserId(1);
        var profile = CreateTestProfile();
        var query = new GetCurrentUserProfileQuery();

        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(userId);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Handle_ShouldReturnAllProfileFields()
    {
        // Arrange
        var userId = new UserId(1);
        var profile = CreateTestProfile();

        // Update profile with more data
        var name = PersonName.Create("Jane", "Smith").Value;
        var bio = Bio.Create("Test bio").Value;
        profile.UpdateBasicInfo(
            name, bio, Location.Empty, ProfessionalRole.Empty, Institution.Empty, null);

        var query = new GetCurrentUserProfileQuery();

        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(userId);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Jane");
        result.Value.Bio.Should().Be("Test bio");
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithNoAuthenticatedUser_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetCurrentUserProfileQuery();

        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.UserId.Returns((UserId?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProfileNotFound_ShouldReturnFailure()
    {
        // Arrange
        var userId = new UserId(1);
        var query = new GetCurrentUserProfileQuery();

        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(userId);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldUseCurrentUserIdToQueryProfile()
    {
        // Arrange
        var userId = new UserId(1);
        var profile = CreateTestProfile();
        var query = new GetCurrentUserProfileQuery();

        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(userId);

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _profileRepository.Received(1).GetByUserIdAsync(
            Arg.Is<UserId>(id => id == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldQueryByUserId()
    {
        // Arrange
        var userId = new UserId(42);
        var profile = CreateTestProfile(42);
        var query = new GetCurrentUserProfileQuery();

        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.UserId.Returns(userId);

        _profileRepository
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _profileRepository.Received(1).GetByUserIdAsync(userId, Arg.Any<CancellationToken>());
    }

    #endregion
}
