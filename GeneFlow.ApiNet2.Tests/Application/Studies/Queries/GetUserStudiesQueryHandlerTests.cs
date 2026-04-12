using GeneFlow.ApiNet2.Application.Studies.Queries.GetUserStudies;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Queries;

/// <summary>
/// Unit tests for GetUserStudiesQueryHandler.
/// </summary>
public class GetUserStudiesQueryHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly GetUserStudiesQueryHandler _handler;

    public GetUserStudiesQueryHandlerTests()
    {
        _handler = new GetUserStudiesQueryHandler(_studyRepository);
    }

    #region Helper Methods

    private static Study CreateTestStudy(
        long studyIdValue,
        UserId ownerId,
        string title = "Test Study")
    {
        var studyId = new StudyId(studyIdValue);
        var titleVo = StudyTitle.Create(title).Value;
        var description = StudyDescription.Create("Test description").Value;

        return Study.Create(studyId, ownerId, titleVo, description, ResearchField.Genomics).Value;
    }

    private static PagedList<Study> CreatePagedList(IEnumerable<Study> studies, int page, int pageSize, int totalCount)
    {
        return PagedList<Study>.Create(studies.ToList(), page, pageSize, totalCount);
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithStudies_ShouldReturnPagedList()
    {
        // Arrange
        var userId = new UserId(1);
        var studies = new[]
        {
            CreateTestStudy(1, userId, "Study 1"),
            CreateTestStudy(2, userId, "Study 2"),
        };

        var pagedStudies = CreatePagedList(studies, 1, 10, 2);
        var query = new GetUserStudiesQuery("U00000001", 1, 10);

        _studyRepository
            .GetByMemberAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<StudyStatus?>(),
                Arg.Any<ResearchField?>(),
                Arg.Any<CancellationToken>())
            .Returns(pagedStudies);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithNoStudies_ShouldReturnEmptyList()
    {
        // Arrange
        var pagedStudies = CreatePagedList(Array.Empty<Study>(), 1, 10, 0);
        var query = new GetUserStudiesQuery("U00000001", 1, 10);

        _studyRepository
            .GetByMemberAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<StudyStatus?>(),
                Arg.Any<ResearchField?>(),
                Arg.Any<CancellationToken>())
            .Returns(pagedStudies);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldPassToRepository()
    {
        // Arrange
        var pagedStudies = CreatePagedList(Array.Empty<Study>(), 1, 10, 0);
        var query = new GetUserStudiesQuery("U00000001", 1, 10, SearchTerm: "cancer");

        _studyRepository
            .GetByMemberAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<StudyStatus?>(),
                Arg.Any<ResearchField?>(),
                Arg.Any<CancellationToken>())
            .Returns(pagedStudies);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).GetByMemberAsync(
            Arg.Any<UserId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            "cancer",
            Arg.Any<StudyStatus?>(),
            Arg.Any<ResearchField?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassToRepository()
    {
        // Arrange
        var pagedStudies = CreatePagedList(Array.Empty<Study>(), 1, 10, 0);
        var query = new GetUserStudiesQuery("U00000001", 1, 10, StatusId: StudyStatus.Active.Id);

        _studyRepository
            .GetByMemberAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<StudyStatus?>(),
                Arg.Any<ResearchField?>(),
                Arg.Any<CancellationToken>())
            .Returns(pagedStudies);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).GetByMemberAsync(
            Arg.Any<UserId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            StudyStatus.Active,
            Arg.Any<ResearchField?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithResearchFieldFilter_ShouldPassToRepository()
    {
        // Arrange
        var pagedStudies = CreatePagedList(Array.Empty<Study>(), 1, 10, 0);
        var query = new GetUserStudiesQuery("U00000001", 1, 10, ResearchFieldId: ResearchField.Genomics.Id);

        _studyRepository
            .GetByMemberAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<StudyStatus?>(),
                Arg.Any<ResearchField?>(),
                Arg.Any<CancellationToken>())
            .Returns(pagedStudies);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).GetByMemberAsync(
            Arg.Any<UserId>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<StudyStatus?>(),
            ResearchField.Genomics,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Pagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var userId = new UserId(1);
        var studies = new[]
        {
            CreateTestStudy(3, userId, "Study Page 2"),
        };

        var pagedStudies = CreatePagedList(studies, 2, 2, 5); // Page 2, 2 per page, 5 total
        var query = new GetUserStudiesQuery("U00000001", 2, 2);

        _studyRepository
            .GetByMemberAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<StudyStatus?>(),
                Arg.Any<ResearchField?>(),
                Arg.Any<CancellationToken>())
            .Returns(pagedStudies);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(3);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserStudiesQuery("invalid-user", 1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidStatusId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetUserStudiesQuery("U00000001", 1, 10, StatusId: 999);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Invalid status returns failure
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatus");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectUserId()
    {
        // Arrange
        var pagedStudies = CreatePagedList(Array.Empty<Study>(), 1, 10, 0);
        var query = new GetUserStudiesQuery("U00000001", 1, 10);

        _studyRepository
            .GetByMemberAsync(
                Arg.Any<UserId>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<StudyStatus?>(),
                Arg.Any<ResearchField?>(),
                Arg.Any<CancellationToken>())
            .Returns(pagedStudies);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).GetByMemberAsync(
            Arg.Is<UserId>(id => id.Value == 1),
            1,
            10,
            Arg.Any<string?>(),
            Arg.Any<StudyStatus?>(),
            Arg.Any<ResearchField?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
