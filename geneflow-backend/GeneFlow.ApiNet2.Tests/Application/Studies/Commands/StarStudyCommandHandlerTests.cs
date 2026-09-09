using GeneFlow.ApiNet2.Application.Studies.Commands.StarStudy;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for StarStudyCommandHandler.
/// </summary>
public class StarStudyCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly StarStudyCommandHandler _handler;

    public StarStudyCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new StarStudyCommandHandler(
            _studyRepository,
            _unitOfWork);
    }

    #region Helper Methods

    private static Study CreateTestStudy(UserId? ownerId = null, bool isPublished = false)
    {
        var studyId = new StudyId(1);
        var userId = ownerId ?? new UserId(1);
        var title = StudyTitle.Create("Test Study").Value;
        var description = StudyDescription.Create("Test description").Value;

        var study = Study.Create(studyId, userId, title, description, ResearchField.Genomics).Value;

        if (isPublished)
        {
            study.ChangeStatus(StudyStatus.Active, userId);
            study.ChangeStatus(StudyStatus.Completed, userId);
            study.ChangeStatus(StudyStatus.Published, userId);
        }

        return study;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_ValidRequest_ShouldStarStudy()
    {
        // Arrange
        var study = CreateTestStudy(isPublished: true);
        var command = new StarStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _studyRepository.Received(1).AddStarAsync(
            Arg.Any<StudyId>(),
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MemberStarsOwnStudy_ShouldSucceed()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        var command = new StarStudyCommand("S00000001", "U00000001"); // Owner stars

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldIncrementStarsCount()
    {
        // Arrange
        var study = CreateTestStudy(isPublished: true);
        var initialStars = study.Metrics.StarsCount;
        var command = new StarStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        study.Metrics.StarsCount.Should().Be(initialStars + 1);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new StarStudyCommand("invalid-id", "U00000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new StarStudyCommand("S00000001", "invalid-user");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new StarStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_AlreadyStarred_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy(isPublished: true);
        var command = new StarStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(true); // Already starred

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AlreadyStarred");
    }

    #endregion

    #region Any User Can Star

    [Fact]
    public async Task Handle_DraftStudy_NonMemberCanStar()
    {
        // Arrange - Handler allows any user to star any study (no membership check)
        var study = CreateTestStudy(); // Draft, not published
        var command = new StarStudyCommand("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Starring is allowed for any existing study
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCheckIfAlreadyStarred()
    {
        // Arrange
        var study = CreateTestStudy(isPublished: true);
        var command = new StarStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).IsStarredByUserAsync(
            Arg.Any<StudyId>(),
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailuREDACTED()
    {
        // Arrange
        var study = CreateTestStudy(isPublished: true);
        var command = new StarStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _studyRepository
            .IsStarredByUserAsync(Arg.Any<StudyId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(true); // Already starred

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _studyRepository.DidNotReceive().AddStarAsync(
            Arg.Any<StudyId>(),
            Arg.Any<UserId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailuREDACTED()
    {
        // Arrange
        var command = new StarStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
