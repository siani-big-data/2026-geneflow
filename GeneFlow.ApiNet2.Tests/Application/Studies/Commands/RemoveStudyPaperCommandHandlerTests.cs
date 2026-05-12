using GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyPaper;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for RemoveStudyPaperCommandHandler.
/// </summary>
public class RemoveStudyPaperCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly RemoveStudyPaperCommandHandler _handler;

    public RemoveStudyPaperCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new RemoveStudyPaperCommandHandler(
            _studyRepository,
            _unitOfWork);
    }

    #region Helper Methods

    private static Study CreateTestStudyWithPaper(out StudyPaperId paperId, UserId? ownerId = null)
    {
        var studyId = new StudyId(1);
        var userId = ownerId ?? new UserId(1);
        var title = StudyTitle.Create("Test Study").Value;
        var description = StudyDescription.Create("Test description").Value;

        var study = Study.Create(studyId, userId, title, description, ResearchField.Genomics).Value;

        // Add a paper
        paperId = new StudyPaperId(1);
        var paper = StudyPaper.Create(
            paperId,
            "Test Paper",
            "John Doe",
            "10.1234/test",
            null, null, 2024,
            null, null, null,
            userId).Value;
        study.AddPaper(paper, userId);

        return study;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldRemovePaper()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudyWithPaper(out var paperId, ownerId);

        var command = new RemoveStudyPaperCommand("S00000001", "R00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AsEditor_ShouldRemovePaper()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editorId = new UserId(2);
        var study = CreateTestStudyWithPaper(out _, ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        var command = new RemoveStudyPaperCommand("S00000001", "R00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new RemoveStudyPaperCommand("invalid-id", "R00000001", "U00000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidPaperId_ShouldReturnFailure()
    {
        // Arrange
        var command = new RemoveStudyPaperCommand("S00000001", "invalid-id", "U00000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PaperNotFound");
    }

    [Fact]
    public async Task Handle_PaperNotFound_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudyWithPaper(out _, ownerId);

        var command = new RemoveStudyPaperCommand("S00000001", "R00000999", "U00000001"); // Non-existent paper

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("PaperNotFound");
    }

    #endregion

    #region Permission Failures

    [Fact]
    public async Task Handle_AsViewer_ShouldReturnInsufficientPermissions()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var study = CreateTestStudyWithPaper(out _, ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var command = new RemoveStudyPaperCommand("S00000001", "R00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InsufficientPermissions");
    }

    [Fact]
    public async Task Handle_AsNonMember_ShouldReturnInsufficientPermissions()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudyWithPaper(out _, ownerId);

        var command = new RemoveStudyPaperCommand("S00000001", "R00000001", "U00000099");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InsufficientPermissions");
    }

    #endregion

    #region Domain Events

    [Fact]
    public async Task Handle_Success_ShouldRaisePaperRemovedEvent()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudyWithPaper(out var paperId, ownerId);
        study.ClearDomainEvents();

        var command = new RemoveStudyPaperCommand("S00000001", "R00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle(e => e is StudyPaperRemovedEvent);
        var evt = study.DomainEvents.OfType<StudyPaperRemovedEvent>().First();
        evt.StudyId.Should().Be(study.Id);
        evt.PaperId.Should().Be(paperId);
        evt.RemovedBy.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_AsAdmin_ShouldRemovePaper()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudyWithPaper(out _, ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new RemoveStudyPaperCommand("S00000001", "R00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new RemoveStudyPaperCommand("S00000001", "R00000001", "invalid-user");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new RemoveStudyPaperCommand("S00000001", "R00000001", "U00000001");

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
}
