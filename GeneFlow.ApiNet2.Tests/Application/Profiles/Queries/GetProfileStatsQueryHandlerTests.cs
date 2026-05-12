using GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfileStats;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Tests.Application.Profiles.Queries;

/// <summary>
/// Unit tests for GetProfileStatsQueryHandler.
/// </summary>
public class GetProfileStatsQueryHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly GetProfileStatsQueryHandler _handler;

    public GetProfileStatsQueryHandlerTests()
    {
        _handler = new GetProfileStatsQueryHandler(
            _profileRepository,
            _studyRepository,
            _traceRepository);
    }

    private static Profile CreateTestProfile(long id = 1)
    {
        var profileId = new ProfileId(id);
        var userId = new UserId(id);
        var name = PersonName.Create("John", "Doe").Value;
        return Profile.Create(profileId, userId, name).Value;
    }

    private static Study CreateTestStudy(long id = 1, long ownerId = 1)
    {
        var studyId = new StudyId(id);
        var ownerUserId = new UserId(ownerId);
        var title = GeneFlow.ApiNet2.Domain.Studies.ValueObjects.StudyTitle.Create("Test Study").Value;
        var description = GeneFlow.ApiNet2.Domain.Studies.ValueObjects.StudyDescription.Create("Test description").Value;
        return Study.Create(studyId, ownerUserId, title, description, GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField.Genomics).Value;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidUserId_ShouldReturnStats()
    {
        // Arrange
        var profile = CreateTestProfile();
        var query = new GetProfileStatsQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(5);

        _studyRepository
            .CountByOwnerIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(3);

        var emptyStudies = PagedList<Study>.Empty(1000);
        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.StudyStatus?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(emptyStudies);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalStudies.Should().Be(5);
        result.Value.OwnedStudies.Should().Be(3);
        result.Value.MemberSince.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ShouldCalculateStudyCount()
    {
        // Arrange
        var profile = CreateTestProfile();
        var query = new GetProfileStatsQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(10);

        _studyRepository
            .CountByOwnerIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(7);

        var emptyStudies = PagedList<Study>.Empty(1000);
        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.StudyStatus?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(emptyStudies);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalStudies.Should().Be(10);
        result.Value.OwnedStudies.Should().Be(7);

        await _studyRepository.Received(1).CountByMemberAsync(
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
        await _studyRepository.Received(1).CountByOwnerIdAsync(
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCalculateTraceCount()
    {
        // Arrange
        var profile = CreateTestProfile();
        var study = CreateTestStudy();
        var query = new GetProfileStatsQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(1);

        _studyRepository
            .CountByOwnerIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var studiesWithItems = PagedList<Study>.Create([study], 1, 1000, 1);
        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.StudyStatus?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(studiesWithItems);

        _traceRepository
            .CountByUserStudiesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns((15, 5)); // 15 processed, 5 pending

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalTraces.Should().Be(20); // 15 + 5

        await _traceRepository.Received(1).CountByUserStudiesAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCalculateMembershipCount()
    {
        // Arrange
        var profile = CreateTestProfile();
        var query = new GetProfileStatsQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        // User is member of 8 studies but owns only 2
        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(8);

        _studyRepository
            .CountByOwnerIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(2);

        var emptyStudies = PagedList<Study>.Empty(1000);
        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.StudyStatus?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(emptyStudies);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Total memberships = TotalStudies (8), owned = 2, so member-only = 6
        result.Value.TotalStudies.Should().Be(8);
        result.Value.OwnedStudies.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithNoStudies_ShouldReturnZeroTraces()
    {
        // Arrange
        var profile = CreateTestProfile();
        var query = new GetProfileStatsQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        _studyRepository
            .CountByOwnerIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var emptyStudies = PagedList<Study>.Empty(1000);
        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.StudyStatus?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(emptyStudies);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalStudies.Should().Be(0);
        result.Value.TotalTraces.Should().Be(0);
        result.Value.LastActivityAt.Should().BeNull();

        // Should not call trace repository when no studies
        await _traceRepository.DidNotReceive().CountByUserStudiesAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnMemberSinceFromProfile()
    {
        // Arrange
        var profile = CreateTestProfile();
        var query = new GetProfileStatsQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        _studyRepository
            .CountByOwnerIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var emptyStudies = PagedList<Study>.Empty(1000);
        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.StudyStatus?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(emptyStudies);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MemberSince.Should().Be(profile.CreatedAt);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithNonExistentProfile_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetProfileStatsQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("P00000001")]
    [InlineData("")]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure(string userId)
    {
        // Arrange
        var query = new GetProfileStatsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldQueryAllRequiredRepositories()
    {
        // Arrange
        var profile = CreateTestProfile();
        var study = CreateTestStudy();
        var query = new GetProfileStatsQuery("U00000001");

        _profileRepository
            .GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(profile);

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(1);

        _studyRepository
            .CountByOwnerIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var studiesWithItems = PagedList<Study>.Create([study], 1, 1000, 1);
        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.StudyStatus?>(), Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(studiesWithItems);

        _traceRepository
            .CountByUserStudiesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns((0, 0));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _profileRepository.Received(1).GetByUserIdAsync(
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
        await _studyRepository.Received(1).CountByMemberAsync(
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
        await _studyRepository.Received(1).CountByOwnerIdAsync(
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
        await _studyRepository.Received(1).GetByMemberAsync(
            Arg.Any<UserId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.StudyStatus?>(),
            Arg.Any<GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
