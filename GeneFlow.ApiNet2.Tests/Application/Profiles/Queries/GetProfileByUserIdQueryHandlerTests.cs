using GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileByUserId;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Queries;

/// <summary>
/// Unit tests for GetProfileByUserIdQueryHandler.
/// </summary>
public class GetProfileByUserIdQueryHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly GetProfileByUserIdQueryHandler _handler;

    public GetProfileByUserIdQueryHandlerTests()
    {
        _handler = new GetProfileByUserIdQueryHandler(_profileRepository);
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
    public async Task Handle_WithExistingProfile_ShouldReturnProfile()
    {
        // Arrange
        var profile = CreateTestProfile();
        var query = new GetProfileByUserIdQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.UserId.Should().Be("U00000001");
    }

    [Fact]
    public async Task Handle_ShouldReturnCompleteProfileDto()
    {
        // Arrange
        var profile = CreateTestProfile();

        // Update profile with additional data
        var bio = Bio.Create("Test bio").Value;
        var location = Location.Create("New York").Value;
        profile.UpdateBasicInfo(
            profile.Name, bio, location, ProfessionalRole.Empty, Institution.Empty, null);

        var query = new GetProfileByUserIdQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Bio.Should().Be("Test bio");
        result.Value.Location.Should().Be("New York");
        result.Value.FullName.Should().Be("John Doe");
        result.Value.Initials.Should().Be("JD");
    }

    #endregion

    #region Failure Cases

    [Theory]
    [InlineData("invalid")]
    [InlineData("P00000001")] // Wrong prefix
    [InlineData("")]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure(string userId)
    {
        // Arrange
        var query = new GetProfileByUserIdQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithProfileNotFound_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetProfileByUserIdQuery("U00000001");

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
    public async Task Handle_ShouldQueryByParsedUserId()
    {
        // Arrange
        var profile = CreateTestProfile(42);
        var query = new GetProfileByUserIdQuery("U00000042");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _profileRepository.Received(1).GetByUserIdAsync(
            Arg.Is<UserId>(id => id.ToString() == "U00000042"),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
