using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyPapers;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Queries;

/// <summary>
/// Unit tests for GetStudyPapersQueryHandler.
/// </summary>
public class GetStudyPapersQueryHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly GetStudyPapersQueryHandler _handler;

    public GetStudyPapersQueryHandlerTests()
    {
        _handler = new GetStudyPapersQueryHandler(_studyRepository);
    }

    #region Helper Methods

    private static Study CreateTestStudy(
        UserId? ownerId = null,
        StudyStatus? status = null)
    {
        var studyId = new StudyId(1);
        var userId = ownerId ?? new UserId(1);
        var title = StudyTitle.Create("Test Study").Value;
        var description = StudyDescription.Create("Test description").Value;

        var study = Study.Create(studyId, userId, title, description, ResearchField.Genomics).Value;

        if (status is not null && status != StudyStatus.Draft)
        {
            study.ChangeStatus(StudyStatus.Active, userId);
            if (status == StudyStatus.Completed || status == StudyStatus.Published)
            {
                study.ChangeStatus(StudyStatus.Completed, userId);
                if (status == StudyStatus.Published)
                {
                    study.ChangeStatus(StudyStatus.Published, userId);
                }
            }
        }

        return study;
    }

    private static StudyPaper CreateTestPaper(UserId userId, int paperId = 1, string title = "Test Paper")
    {
        return StudyPaper.Create(
            new StudyPaperId(paperId),
            title,
            "John Doe",
            "10.1234/test",
            "Abstract text",
            "Nature",
            2024,
            null, null, null,
            userId).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnPapers()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        var paper = CreateTestPaper(ownerId);
        study.AddPaper(paper, ownerId);

        var query = new GetStudyPapersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Title.Should().Be("Test Paper");
    }

    [Fact]
    public async Task Handle_PublishedStudy_NonMemberCanViewPapers()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId, StudyStatus.Published);
        var paper = CreateTestPaper(ownerId);
        study.AddPaper(paper, ownerId);

        var query = new GetStudyPapersQuery("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldNotReturnDeletedPapers()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var paper1 = CreateTestPaper(ownerId, 1, "Active Paper");
        var paper2 = CreateTestPaper(ownerId, 2, "Deleted Paper");

        study.AddPaper(paper1, ownerId);
        study.AddPaper(paper2, ownerId);
        study.RemovePaper(new StudyPaperId(2), ownerId); // Soft delete

        var query = new GetStudyPapersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Title.Should().Be("Active Paper");
    }

    [Fact]
    public async Task Handle_EmptyPapers_ShouldReturnEmptyList()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var query = new GetStudyPapersQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyPapersQuery("invalid-id", "U00000001");

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
        var query = new GetStudyPapersQuery("S00000001", "U00000001");

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

    #region Permission Failures

    [Fact]
    public async Task Handle_DraftStudy_NonMemberCannotView()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId); // Draft status

        var query = new GetStudyPapersQuery("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion
}
