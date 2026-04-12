using GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyMember;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for AddStudyMemberCommandHandler.
/// </summary>
public class AddStudyMemberCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly AddStudyMemberCommandHandler _handler;

    public AddStudyMemberCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new AddStudyMemberCommandHandler(
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
    public async Task Handle_ByOwner_ShouldAddMember()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001", // Owner
            "U00000002", // New member
            StudyRole.Editor.Id);

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
    public async Task Handle_ByAdmin_ShouldAddMember()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000002", // Admin
            "U00000003", // New member
            StudyRole.Viewer.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AddAsEditor_ShouldSetEditorRole()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Editor.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(new UserId(2))!.Role.Should().Be(StudyRole.Editor);
    }

    [Fact]
    public async Task Handle_AddAsViewer_ShouldSetViewerRole()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Viewer.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(new UserId(2))!.Role.Should().Be(StudyRole.Viewer);
    }

    [Fact]
    public async Task Handle_AddAsAdmin_ShouldSetAdminRole()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Admin.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(new UserId(2))!.Role.Should().Be(StudyRole.Admin);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddStudyMemberCommand(
            "invalid",
            "U00000001",
            "U00000002",
            StudyRole.Editor.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidRequesterId_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddStudyMemberCommand(
            "S00000001",
            "invalid",
            "U00000002",
            StudyRole.Editor.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidNewMemberId_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "invalid",
            StudyRole.Editor.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidRole_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            999); // Invalid role

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Editor.Id);

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

        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000002", // Editor
            "U00000003",
            StudyRole.Viewer.Id);

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

        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000002", // Viewer
            "U00000003",
            StudyRole.Viewer.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AddAsOwner_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Owner.Id); // Cannot add as owner

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AlreadyMember_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var existingMemberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(existingMemberId, StudyRole.Editor, ownerId);

        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002", // Already a member
            StudyRole.Viewer.Id);

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
    public async Task Handle_Success_ShouldPersistChanges()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Editor.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailuREDACTED()
    {
        // Arrange
        var command = new AddStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Editor.Id);

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
