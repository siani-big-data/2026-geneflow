using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyById;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Queries;

/// <summary>
/// Unit tests for GetStudyByIdQueryHandler.
/// </summary>
public class GetStudyByIdQueryHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly GetStudyByIdQueryHandler _handler;

    public GetStudyByIdQueryHandlerTests()
    {
        _handler = new GetStudyByIdQueryHandler(_studyRepository);
    }

    #region Helper Methods

    private static Study CreateTestStudy(
        UserId? ownerId = null,
        string title = "Test Study",
        StudyStatus? status = null)
    {
        var studyId = new StudyId(1);
        var userId = ownerId ?? new UserId(1);
        var titleVo = StudyTitle.Create(title).Value;
        var description = StudyDescription.Create("Test description").Value;

        var study = Study.Create(studyId, userId, titleVo, description, ResearchField.Genomics).Value;

        if (status is not null && status != StudyStatus.Draft)
        {
            // Transition through valid states
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

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_StudyExists_ShouldReturnStudyDto()
    {
        // Arrange
        var study = CreateTestStudy(title: "Genomic Analysis");
        var query = new GetStudyByIdQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Genomic Analysis");
        result.Value.ResearchField.Should().Be("Genomics");
    }

    [Fact]
    public async Task Handle_AsMember_ShouldReturnStudy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var query = new GetStudyByIdQuery("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PublishedStudy_AnyUserCanView()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId, status: StudyStatus.Published);

        var query = new GetStudyByIdQuery("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectMetrics()
    {
        // Arrange
        var study = CreateTestStudy();
        study.IncrementViews();
        study.IncrementViews();
        study.IncrementStars();

        var query = new GetStudyByIdQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ViewsCount.Should().Be(2);
        result.Value.StarsCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnMembers()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddMember(new UserId(2), StudyRole.Editor, ownerId);
        study.AddMember(new UserId(3), StudyRole.Viewer, ownerId);

        var query = new GetStudyByIdQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnTags()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddTag("genomics", ownerId);
        study.AddTag("cancer", ownerId);

        var query = new GetStudyByIdQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Should().Contain(new[] { "genomics", "cancer" });
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyByIdQuery("invalid-id", "U00000001");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyByIdQuery("S00000001", "invalid-user");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetStudyByIdQuery("S00000001", "U00000001");

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

        var query = new GetStudyByIdQuery("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ActiveStudy_NonMemberCannotView()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.ChangeStatus(StudyStatus.Active, ownerId);

        var query = new GetStudyByIdQuery("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var study = CreateTestStudy();
        var query = new GetStudyByIdQuery("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).GetByIdAsync(
            Arg.Is<StudyId>(id => id.Value == 1),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
