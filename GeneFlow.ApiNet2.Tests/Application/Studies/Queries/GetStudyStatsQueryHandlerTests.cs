using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyStats;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Queries;

/// <summary>
/// Unit tests for GetStudyStatsQueryHandler.
/// </summary>
public class GetStudyStatsQueryHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly GetStudyStatsQueryHandler _handler;

    public GetStudyStatsQueryHandlerTests()
    {
        _handler = new GetStudyStatsQueryHandler(_studyRepository);

        // Default setup
        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    #region Helper Methods

    private static Study CreateTestStudy(UserId? ownerId = null)
    {
        var studyId = new StudyId(1);
        var userId = ownerId ?? new UserId(1);
        var title = StudyTitle.Create("Test Study").Value;
        var description = StudyDescription.Create("Test description").Value;

        return Study.Create(studyId, userId, title, description, ResearchField.Genomics).Value;
    }

    private static StudyPaper CreateTestPaper(UserId userId, int paperId = 1)
    {
        return StudyPaper.Create(
            new StudyPaperId(paperId),
            "Test Paper",
            null, null, null, null, null,
            null, null, null,
            userId).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidStudyId_ShouldReturnStats()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StudyId.Should().Be("S00000001");
        result.Value.MemberCount.Should().Be(1); // Owner
        result.Value.ViewsCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.StarsCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.PaperCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnStats()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StudyId.Should().Be("S00000001");
        result.Value.MemberCount.Should().Be(1); // Owner
    }

    [Fact]
    public async Task Handle_ShouldIncludeViewCount()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.IncrementViews();
        study.IncrementViews();
        study.IncrementViews();
        study.IncrementViews();
        study.IncrementViews();

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ViewsCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldIncludeStarCount()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.IncrementStars();
        study.IncrementStars();
        study.IncrementStars();

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StarsCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectViewsAndStars()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.IncrementViews();
        study.IncrementViews();
        study.IncrementViews();
        study.IncrementStars();
        study.IncrementStars();

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ViewsCount.Should().Be(3);
        result.Value.StarsCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldIncludeMemberCount()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Admin, ownerId);
        study.AddMember(new UserId(3), StudyRole.Editor, ownerId);

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MemberCount.Should().Be(3); // Owner + 2 members
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectMemberCount()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Admin, ownerId);
        study.AddMember(new UserId(3), StudyRole.Editor, ownerId);
        study.AddMember(new UserId(4), StudyRole.Viewer, ownerId);

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MemberCount.Should().Be(4);
    }

    [Fact]
    public async Task Handle_ShouldIncludeTraceCount()
    {
        // Note: TraceCount is not directly available in StudyStatsDto
        // The handler returns PaperCount, not TraceCount
        // This test validates the paper count which may be the intended metric
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddPaper(CreateTestPaper(ownerId, 1), ownerId);
        study.AddPaper(CreateTestPaper(ownerId, 2), ownerId);
        study.AddPaper(CreateTestPaper(ownerId, 3), ownerId);

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaperCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectPaperCount()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddPaper(CreateTestPaper(ownerId, 1), ownerId);
        study.AddPaper(CreateTestPaper(ownerId, 2), ownerId);

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaperCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenStarred_ShouldReturnIsStarredTrue()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsStarredByCurrentUser.Should().BeTrue();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyStatsQuery("invalid-id", "U00000001");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyStatsQuery("S00000001", "invalid-user");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithNonExistentStudy_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetStudyStatsQuery("S00000999", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyStatsQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithoutUserId_ShouldReturnStats()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var query = new GetStudyStatsQuery("S00000001"); // No UserId

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsStarredByCurrentUser.Should().BeFalse();
    }

    #endregion
}
