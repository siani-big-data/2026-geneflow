using GeneFlow.ApiNet2.Application.Studies.Commands.CancelInvitation;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for CancelInvitationCommandHandler.
/// </summary>
public class CancelInvitationCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyInvitationRepository _invitationRepository = Substitute.For<IStudyInvitationRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly CancelInvitationCommandHandler _handler;

    public CancelInvitationCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new CancelInvitationCommandHandler(
            _studyRepository,
            _invitationRepository,
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

    private static StudyInvitation CreateTestInvitation(
        StudyId? studyId = null,
        StudyRole? role = null,
        UserId? invitedBy = null)
    {
        var id = new StudyInvitationId(1);
        var sId = studyId ?? new StudyId(1);
        var r = role ?? StudyRole.Editor;
        var by = invitedBy ?? new UserId(1);

        return StudyInvitation.Create(id, sId, "test@example.com", r, by).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_ByOwner_ShouldCancelInvitation()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001"); // Owner

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_ByAdmin_ShouldCancelInvitation()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var invitation = CreateTestInvitation();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000002"); // Admin

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Success_ShouldPersistChanges()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelInvitationCommand(
            "invalid",
            "I00000001",
            "U00000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidInvitationId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelInvitationCommand(
            "S00000001",
            "invalid",
            "U00000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvitationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns((StudyInvitation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationNotFound");
    }

    [Fact]
    public async Task Handle_InvitationAlreadyAccepted_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        invitation.Accept(); // Already accepted
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
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

        var invitation = CreateTestInvitation();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000002"); // Editor - cannot manage members

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InsufficientPermissions");
    }

    [Fact]
    public async Task Handle_ByViewer_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var invitation = CreateTestInvitation();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000002"); // Viewer - cannot manage members

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InsufficientPermissions");
    }

    [Fact]
    public async Task Handle_ByNonMember_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000099"); // Not a member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InsufficientPermissions");
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "invalid"); // Invalid user ID format

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
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

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
    public async Task Handle_InvitationFromDifferentStudy_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation(studyId: new StudyId(999)); // Different study
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationNotFound");
    }

    [Fact]
    public async Task Handle_WithDeclinedInvitation_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        invitation.Decline(); // Already declined
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
    }

    [Fact]
    public async Task Handle_WithAlreadyCancelled_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        invitation.Cancel(); // Already cancelled
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
    }

    [Fact]
    public async Task Handle_Success_ShouldSetStatusToCancelled()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_Success_ShouldReturnSuccessResult()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new CancelInvitationCommand(
            "S00000001",
            "I00000001",
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .GetByIdAsync(Arg.Any<StudyInvitationId>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    #endregion
}
