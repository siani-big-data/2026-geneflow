using GeneFlow.ApiNet2.Application.Usage.Queries.GetDashboardStats;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Tests.Application.Usage.Queries;

/// <summary>
/// Unit tests for GetDashboardStatsQueryHandler.
/// </summary>
public class GetDashboardStatsQueryHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly GetDashboardStatsQueryHandler _handler;

    public GetDashboardStatsQueryHandlerTests()
    {
        _handler = new GetDashboardStatsQueryHandler(_studyRepository, _traceRepository);
    }

    #region Helper Methods

    private static long _studySequence = 1;

    private static Study CreateTestStudy(UserId ownerId)
    {
        var studyId = StudyId.FromSequence(_studySequence++);
        var title = StudyTitle.Create("Test Study").Value;
        var description = StudyDescription.Create("Test description").Value;

        return Study.Create(studyId, ownerId, title, description, ResearchField.Genetics).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidUserId_ShouldReturnDashboardStats()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var query = new GetDashboardStatsQuery(userId.ToString());

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(5);

        _studyRepository
            .CountMembersInUserStudiesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(12);

        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<StudyStatus?>(), Arg.Any<ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(PagedList<Study>.Create(new List<Study>(), 1, 1000, 0));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveStudies.Should().Be(5);
        result.Value.TeamActivity.Should().Be(12);
    }

    [Fact]
    public async Task Handle_WithStudies_ShouldReturnTraceCounts()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var study = CreateTestStudy(userId);
        var query = new GetDashboardStatsQuery(userId.ToString());

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(1);

        _studyRepository
            .CountMembersInUserStudiesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(3);

        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<StudyStatus?>(), Arg.Any<ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(PagedList<Study>.Create(new List<Study> { study }, 1, 1000, 1));

        _traceRepository
            .CountByUserStudiesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns((100, 5));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProcessedTraces.Should().Be(100);
        result.Value.PendingTraces.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithNoStudies_ShouldReturnZeroTraceCounts()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var query = new GetDashboardStatsQuery(userId.ToString());

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        _studyRepository
            .CountMembersInUserStudiesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<StudyStatus?>(), Arg.Any<ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(PagedList<Study>.Create(new List<Study>(), 1, 1000, 0));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProcessedTraces.Should().Be(0);
        result.Value.PendingTraces.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnAlignmentsCompletedAsZero()
    {
        // Arrange - AlignmentsCompleted is currently hardcoded to 0
        var userId = UserId.FromSequence(1);
        var query = new GetDashboardStatsQuery(userId.ToString());

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        _studyRepository
            .CountMembersInUserStudiesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<StudyStatus?>(), Arg.Any<ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(PagedList<Study>.Create(new List<Study>(), 1, 1000, 0));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AlignmentsCompleted.Should().Be(0);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetDashboardStatsQuery("invalid-user-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("UserNotFound");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("X00000001")]
    [InlineData("U123")]
    public async Task Handle_WithMalformedUserId_ShouldReturnFailure(string userId)
    {
        // Arrange
        var query = new GetDashboardStatsQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallStudyRepositoryMethods()
    {
        // Arrange
        var userId = UserId.FromSequence(1);
        var query = new GetDashboardStatsQuery(userId.ToString());

        _studyRepository
            .CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        _studyRepository
            .CountMembersInUserStudiesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(0);

        _studyRepository
            .GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<StudyStatus?>(), Arg.Any<ResearchField?>(), Arg.Any<CancellationToken>())
            .Returns(PagedList<Study>.Create(new List<Study>(), 1, 1000, 0));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).CountByMemberAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
        await _studyRepository.Received(1).CountMembersInUserStudiesAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
        await _studyRepository.Received(1).GetByMemberAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<StudyStatus?>(), Arg.Any<ResearchField?>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
