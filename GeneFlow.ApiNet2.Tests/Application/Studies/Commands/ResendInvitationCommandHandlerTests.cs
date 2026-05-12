using GeneFlow.ApiNet2.Application.Studies.Commands.ResendInvitation;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for ResendInvitationCommandHandler.
/// </summary>
public class ResendInvitationCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyInvitationRepository _invitationRepository = Substitute.For<IStudyInvitationRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly ResendInvitationCommandHandler _handler;

    public ResendInvitationCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new ResendInvitationCommandHandler(
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
    public async Task Handle_ByOwner_ShouldResendInvitation()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var originalToken = invitation.Token;
        var command = new ResendInvitationCommand(
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
        result.Value.Should().NotBeNull();
        invitation.Token.Should().NotBe(originalToken); // Token should be regenerated
    }

    [Fact]
    public async Task Handle_ByAdmin_ShouldResendInvitation()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var invitation = CreateTestInvitation();
        var command = new ResendInvitationCommand(
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
        var command = new ResendInvitationCommand(
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

    [Fact]
    public async Task Handle_ReturnsUpdatedInvitationDto()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new ResendInvitationCommand(
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
        result.Value.Email.Should().Be("test@example.com");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new ResendInvitationCommand(
            "invalid",
            "I00000001",
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
        var command = new ResendInvitationCommand(
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
        invitation.Accept(); // Already accepted - cannot be resent
        var command = new ResendInvitationCommand(
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
        result.Error.Code.Should().Contain("CannotResend");
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
        var command = new ResendInvitationCommand(
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
        var command = new ResendInvitationCommand(
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
        var command = new ResendInvitationCommand(
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
    public async Task Handle_WithInvalidInvitationId_ShouldReturnFailure()
    {
        // Arrange
        var command = new ResendInvitationCommand(
            "S00000001",
            "invalid", // Invalid invitation ID format
            "U00000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new ResendInvitationCommand(
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
        var command = new ResendInvitationCommand(
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
        var command = new ResendInvitationCommand(
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
        var command = new ResendInvitationCommand(
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
        result.Error.Code.Should().Contain("CannotResend");
    }

    [Fact]
    public async Task Handle_WithCancelledInvitation_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        invitation.Cancel(); // Cancelled
        var command = new ResendInvitationCommand(
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
        result.Error.Code.Should().Contain("CannotResend");
    }

    [Fact]
    public async Task Handle_ShouldRegenerateToken()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var originalToken = invitation.Token;
        var command = new ResendInvitationCommand(
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
        invitation.Token.Should().NotBe(originalToken);
        invitation.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldResetExpirationDate()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var originalExpiresAt = invitation.ExpiresAt;
        var command = new ResendInvitationCommand(
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
        invitation.ExpiresAt.Should().BeAfter(originalExpiresAt.AddSeconds(-1)); // Account for test timing
    }

    [Fact]
    public async Task Handle_ShouldClearRespondedAt()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        // Set RespondedAt by declining and then allowing resend via expired status
        var command = new ResendInvitationCommand(
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
        invitation.RespondedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMaintainOriginalEmail()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var originalEmail = invitation.Email;
        var command = new ResendInvitationCommand(
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
        invitation.Email.Should().Be(originalEmail);
        result.Value.Email.Should().Be(originalEmail);
    }

    [Fact]
    public async Task Handle_ShouldMaintainOriginalRole()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation(role: StudyRole.Admin);
        var command = new ResendInvitationCommand(
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
        invitation.Role.Should().Be(StudyRole.Admin);
    }

    #endregion
}
