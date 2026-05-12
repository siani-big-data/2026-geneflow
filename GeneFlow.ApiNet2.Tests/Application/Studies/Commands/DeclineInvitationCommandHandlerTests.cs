using GeneFlow.ApiNet2.Application.Studies.Commands.DeclineInvitation;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for DeclineInvitationCommandHandler.
/// </summary>
public class DeclineInvitationCommandHandlerTests
{
    private readonly IStudyInvitationRepository _invitationRepository = Substitute.For<IStudyInvitationRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly DeclineInvitationCommandHandler _handler;

    public DeclineInvitationCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new DeclineInvitationCommandHandler(
            _invitationRepository,
            _unitOfWork);
    }

    #region Helper Methods

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

    private static StudyInvitation CreateExpiredInvitation(
        StudyId? studyId = null,
        StudyRole? role = null,
        UserId? invitedBy = null)
    {
        var id = new StudyInvitationId(2);
        var sId = studyId ?? new StudyId(1);
        var r = role ?? StudyRole.Editor;
        var by = invitedBy ?? new UserId(1);

        var invitation = StudyInvitation.Create(id, sId, "expired@example.com", r, by).Value;

        // Use reflection to set the ExpiresAt to a past date
        var expiresAtProperty = typeof(StudyInvitation).GetProperty("ExpiresAt");
        expiresAtProperty?.SetValue(invitation, DateTime.UtcNow.AddDays(-1));

        return invitation;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidToken_ShouldDeclineInvitation()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Declined);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldSetRespondedAt()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.RespondedAt.Should().NotBeNull();
        invitation.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_Success_ShouldPersistChanges()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_ShouldReturnSuccessResult()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithEditorRole_ShouldDecline()
    {
        // Arrange
        var invitation = CreateTestInvitation(role: StudyRole.Editor);
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Declined);
    }

    [Fact]
    public async Task Handle_WithAdminRole_ShouldDecline()
    {
        // Arrange
        var invitation = CreateTestInvitation(role: StudyRole.Admin);
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Declined);
    }

    [Fact]
    public async Task Handle_WithViewerRole_ShouldDecline()
    {
        // Arrange
        var invitation = CreateTestInvitation(role: StudyRole.Viewer);
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Declined);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeclineInvitationCommand("invalid-token");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((StudyInvitation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationNotFound");
    }

    [Fact]
    public async Task Handle_WithEmptyToken_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeclineInvitationCommand("");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((StudyInvitation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationNotFound");
    }

    [Fact]
    public async Task Handle_InvitationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeclineInvitationCommand("non-existent-token");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((StudyInvitation?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationNotFound");
    }

    [Fact]
    public async Task Handle_WithExpiredInvitation_ShouldReturnFailure()
    {
        // Arrange
        var invitation = CreateExpiredInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationExpired");
    }

    [Fact]
    public async Task Handle_InvitationAlreadyAccepted_ShouldReturnFailure()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        invitation.Accept(); // Already accepted
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
    }

    [Fact]
    public async Task Handle_InvitationAlreadyDeclined_ShouldReturnFailure()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        invitation.Decline(); // Already declined
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
    }

    [Fact]
    public async Task Handle_InvitationAlreadyCancelled_ShouldReturnFailure()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        invitation.Cancel(); // Already cancelled
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldNotModifyStudy()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var originalStudyId = invitation.StudyId;
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.StudyId.Should().Be(originalStudyId);
    }

    [Fact]
    public async Task Handle_ShouldPreserveEmail()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var originalEmail = invitation.Email;
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Email.Should().Be(originalEmail);
    }

    [Fact]
    public async Task Handle_ShouldPreserveRole()
    {
        // Arrange
        var invitation = CreateTestInvitation(role: StudyRole.Admin);
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Role.Should().Be(StudyRole.Admin);
    }

    [Fact]
    public async Task Handle_ShouldPreserveInvitedBy()
    {
        // Arrange
        var inviterId = new UserId(5);
        var invitation = CreateTestInvitation(invitedBy: inviterId);
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.InvitedBy.Should().Be(inviterId);
    }

    [Fact]
    public async Task Handle_FailuREDACTED()
    {
        // Arrange
        var command = new DeclineInvitationCommand("invalid-token");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((StudyInvitation?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithLongToken_ShouldSearchCorrectly()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(invitation.Token, Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _invitationRepository.Received(1).GetByTokenAsync(invitation.Token, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldCallGetByTokenAsync()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _invitationRepository.Received(1).GetByTokenAsync(
            invitation.Token,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_ShouldCallSaveChangesOnce()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new DeclineInvitationCommand(invitation.Token);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        // Act
        await _handler.Handle(command, token);

        // Assert
        await _invitationRepository.Received(1).GetByTokenAsync(
            Arg.Any<string>(),
            token);
        await _unitOfWork.Received(1).SaveChangesAsync(token);
    }

    #endregion
}
