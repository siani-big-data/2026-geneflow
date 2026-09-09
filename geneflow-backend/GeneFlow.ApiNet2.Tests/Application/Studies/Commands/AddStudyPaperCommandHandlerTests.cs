using GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyPaper;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for AddStudyPaperCommandHandler.
/// </summary>
public class AddStudyPaperCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly AddStudyPaperCommandHandler _handler;

    public AddStudyPaperCommandHandlerTests()
    {
        _sequenceGenerator
            .NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(1L));

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new AddStudyPaperCommandHandler(
            _studyRepository,
            _unitOfWork,
            _sequenceGenerator);
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
    public async Task Handle_WithValidData_ShouldAddPaper()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000001",
            "A Novel Approach to Gene Sequencing");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("A Novel Approach to Gene Sequencing");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAllFields_ShouldAddPaperWithAllData()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000001",
            "Complete Paper Title",
            Authors: "John Doe, Jane Smith",
            Doi: "10.1234/example.doi",
            Abstract: "This is the abstract of the paper.",
            Journal: "Nature Genetics",
            PublicationYear: 2024,
            FileId: "file-123",
            FileName: "paper.pdf",
            FileSizeBytes: 1024000);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Complete Paper Title");
        result.Value.Authors.Should().Be("John Doe, Jane Smith");
        result.Value.Doi.Should().Be("10.1234/example.doi");
        result.Value.Journal.Should().Be("Nature Genetics");
        result.Value.PublicationYear.Should().Be(2024);
        result.Value.HasFile.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AsEditor_ShouldAddPaper()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editorId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000002",
            "Editor's Paper");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Editor's Paper");
    }

    [Fact]
    public async Task Handle_AsAdmin_ShouldAddPaper()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000002",
            "Admin's Paper");

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
        var command = new AddStudyPaperCommand(
            "invalid-study-id",
            "U00000001",
            "Test Paper");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddStudyPaperCommand(
            "S00000001",
            "invalid-user-id",
            "Test Paper");

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
        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000001",
            "Test Paper");

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
    public async Task Handle_AsViewer_ShouldReturnInsufficientPermissions()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000002",
            "Viewer's Paper");

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
        var study = CreateTestStudy(ownerId);

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000099", // Non-member
            "Non-member's Paper");

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

    #region Interactions

    [Fact]
    public async Task Handle_ShouldGenerateSequenceId()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000001",
            "Test Paper");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(
            StudyPaperId.SequenceName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMaxPapersReached_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        // Add maximum number of papers
        for (int i = 1; i <= Study.MaxPapers; i++)
        {
            var paperId = new StudyPaperId(i);
            var paper = StudyPaper.Create(
                paperId,
                $"Paper {i}",
                "Author",
                null, null, null, 2024,
                null, null, null,
                ownerId).Value;
            study.AddPaper(paper, ownerId);
        }

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000001",
            "One More Paper");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("MaxPapersReached");
    }

    [Fact]
    public async Task Handle_Success_ShouldRaisePaperAddedEvent()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.ClearDomainEvents();

        var command = new AddStudyPaperCommand(
            "S00000001",
            "U00000001",
            "Test Paper for Event",
            Doi: "10.1234/test.event");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle(e => e is StudyPaperAddedEvent);
        var evt = study.DomainEvents.OfType<StudyPaperAddedEvent>().First();
        evt.StudyId.Should().Be(study.Id);
        evt.Title.Should().Be("Test Paper for Event");
        evt.Doi.Should().Be("10.1234/test.event");
        evt.AddedBy.Should().Be(ownerId);
    }

    #endregion
}
