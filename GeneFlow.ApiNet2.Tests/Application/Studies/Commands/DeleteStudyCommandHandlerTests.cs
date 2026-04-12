using GeneFlow.ApiNet2.Application.Studies.Commands.DeleteStudy;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for DeleteStudyCommandHandler.
/// </summary>
public class DeleteStudyCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly DeleteStudyCommandHandler _handler;

    public DeleteStudyCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new DeleteStudyCommandHandler(
            _studyRepository,
            _unitOfWork);
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

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_ByOwner_ShouldDeleteStudy()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new DeleteStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsDeleted.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ByAdmin_ShouldReturnFailure()
    {
        // Arrange - Only owner can delete, not admin
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new DeleteStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteStudyCommand("invalid-id", "U00000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteStudyCommand("S00000001", "invalid-user");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Permission Failures

    [Fact]
    public async Task Handle_ByEditor_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editorId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        var command = new DeleteStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ByViewer_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var command = new DeleteStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ByNonMember_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy(new UserId(1));
        var command = new DeleteStudyCommand("S00000001", "U00000099");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldFetchStudyFirst()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new DeleteStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).GetByIdAsync(
            Arg.Any<StudyId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_ShouldSoftDeleteStudy()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new DeleteStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        study.IsDeleted.Should().BeTrue();
        study.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_FailuREDACTED()
    {
        // Arrange
        var ownerId = new UserId(1);
        var nonOwnerId = new UserId(99);
        var study = CreateTestStudy(ownerId);
        var command = new DeleteStudyCommand("S00000001", "U00000099");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        study.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FailuREDACTED()
    {
        // Arrange
        var command = new DeleteStudyCommand("S00000001", "U00000001");

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
