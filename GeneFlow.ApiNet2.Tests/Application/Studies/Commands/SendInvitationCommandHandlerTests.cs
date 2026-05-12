using GeneFlow.ApiNet2.Application.Studies.Commands.SendInvitation;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for SendInvitationCommandHandler.
/// </summary>
public class SendInvitationCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyInvitationRepository _invitationRepository = Substitute.For<IStudyInvitationRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly SendInvitationCommandHandler _handler;

    public SendInvitationCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _sequenceGenerator
            .NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        _handler = new SendInvitationCommandHandler(
            _studyRepository,
            _invitationRepository,
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
    public async Task Handle_ByOwner_ShouldSendInvitation()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new SendInvitationCommand(
            "S00000001",
            "U00000001", // Owner
            "invited@example.com",
            StudyRole.Editor.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .HasPendingInvitationAsync(Arg.Any<StudyId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be("invited@example.com");
    }

    [Fact]
    public async Task Handle_ByAdmin_ShouldSendInvitation()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new SendInvitationCommand(
            "S00000001",
            "U00000002", // Admin
            "invited@example.com",
            StudyRole.Viewer.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .HasPendingInvitationAsync(Arg.Any<StudyId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithMessage_ShouldIncludeMessage()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new SendInvitationCommand(
            "S00000001",
            "U00000001",
            "invited@example.com",
            StudyRole.Editor.Id,
            "Welcome to our study!");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .HasPendingInvitationAsync(Arg.Any<StudyId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Message.Should().Be("Welcome to our study!");
    }

    [Fact]
    public async Task Handle_Success_ShouldPersistInvitation()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new SendInvitationCommand(
            "S00000001",
            "U00000001",
            "invited@example.com",
            StudyRole.Editor.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .HasPendingInvitationAsync(Arg.Any<StudyId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _invitationRepository.Received(1).AddAsync(Arg.Any<StudyInvitation>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new SendInvitationCommand(
            "invalid",
            "U00000001",
            "invited@example.com",
            StudyRole.Editor.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new SendInvitationCommand(
            "S00000001",
            "invalid",
            "invited@example.com",
            StudyRole.Editor.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidRole_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new SendInvitationCommand(
            "S00000001",
            "U00000001",
            "invited@example.com",
            999); // Invalid role ID

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidRole");
    }

    [Fact]
    public async Task Handle_WithOwnerRole_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new SendInvitationCommand(
            "S00000001",
            "U00000001",
            "invited@example.com",
            StudyRole.Owner.Id); // Cannot invite as owner

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidRole");
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new SendInvitationCommand(
            "S00000001",
            "U00000001",
            "invited@example.com",
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

    [Fact]
    public async Task Handle_DuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new SendInvitationCommand(
            "S00000001",
            "U00000001",
            "already-invited@example.com",
            StudyRole.Editor.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        _invitationRepository
            .HasPendingInvitationAsync(Arg.Any<StudyId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true); // Already has pending invitation

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyExists");
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

        var command = new SendInvitationCommand(
            "S00000001",
            "U00000002", // Editor - cannot manage members
            "invited@example.com",
            StudyRole.Viewer.Id);

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
    public async Task Handle_ByViewer_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var command = new SendInvitationCommand(
            "S00000001",
            "U00000002", // Viewer - cannot manage members
            "invited@example.com",
            StudyRole.Viewer.Id);

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
    public async Task Handle_ByNonMember_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new SendInvitationCommand(
            "S00000001",
            "U00000099", // Not a member
            "invited@example.com",
            StudyRole.Editor.Id);

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
}
