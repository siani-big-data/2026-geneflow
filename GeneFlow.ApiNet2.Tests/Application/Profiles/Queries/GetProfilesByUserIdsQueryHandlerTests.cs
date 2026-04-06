using GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfilesByUserIds;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Queries;

/// <summary>
/// Unit tests for GetProfilesByUserIdsQueryHandler.
/// </summary>
public class GetProfilesByUserIdsQueryHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly GetProfilesByUserIdsQueryHandler _handler;

    public GetProfilesByUserIdsQueryHandlerTests()
    {
        _handler = new GetProfilesByUserIdsQueryHandler(_profileRepository);
    }

    private static Profile CreateTestProfile(long id, string firstName, string? lastName = null)
    {
        var profileId = new ProfileId(id);
        var userId = new UserId(id);
        var name = PersonName.Create(firstName, lastName).Value;
        return Profile.Create(profileId, userId, name).Value;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidUserIds_ShouldReturnProfiles()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            CreateTestProfile(1, "John", "Doe"),
            CreateTestProfile(2, "Jane", "Smith")
        };

        var query = new GetProfilesByUserIdsQuery(new[] { "U00000001", "U00000002" });

        _profileRepository
            .GetByUserIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(profiles);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(p => p.FullName == "John Doe");
        result.Value.Should().Contain(p => p.FullName == "Jane Smith");
    }

    [Fact]
    public async Task Handle_WithEmptyList_ShouldReturnEmptyResult()
    {
        // Arrange
        var query = new GetProfilesByUserIdsQuery(Array.Empty<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidUserIds_ShouldSkipThem()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            CreateTestProfile(1, "John", "Doe")
        };

        var query = new GetProfilesByUserIdsQuery(new[] { "U00000001", "invalid", "P00000002" });

        _profileRepository
            .GetByUserIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(profiles);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithAllInvalidUserIds_ShouldReturnEmpty()
    {
        // Arrange
        var query = new GetProfilesByUserIdsQuery(new[] { "invalid1", "invalid2" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSomeProfilesNotFound_ShouldReturnFoundOnes()
    {
        // Arrange
        var profiles = new List<Profile>
        {
            CreateTestProfile(1, "John", "Doe")
        };

        var query = new GetProfilesByUserIdsQuery(new[] { "U00000001", "U00000002", "U00000003" });

        _profileRepository
            .GetByUserIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(profiles);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    #endregion

    #region Return Summary DTOs

    [Fact]
    public async Task Handle_ShouldReturnSummaryDtos()
    {
        // Arrange
        var profile = CreateTestProfile(1, "John", "Doe");
        var profiles = new List<Profile> { profile };

        var query = new GetProfilesByUserIdsQuery(new[] { "U00000001" });

        _profileRepository
            .GetByUserIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(profiles);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.Id.Should().Be("P00000001");
        dto.UserId.Should().Be("U00000001");
        dto.FullName.Should().Be("John Doe");
        dto.Initials.Should().Be("JD");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldQueryRepositoryWithValidUserIds()
    {
        // Arrange
        var query = new GetProfilesByUserIdsQuery(new[] { "U00000001", "U00000002" });

        _profileRepository
            .GetByUserIdsAsync(Arg.Any<IEnumerable<UserId>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Profile>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _profileRepository.Received(1).GetByUserIdsAsync(
            Arg.Is<IEnumerable<UserId>>(ids => ids.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
