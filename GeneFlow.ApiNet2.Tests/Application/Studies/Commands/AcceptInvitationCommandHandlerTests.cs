using GeneFlow.ApiNet2.Application.Studies.Commands.AcceptInvitation;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for AcceptInvitationCommandHandler.
/// </summary>
public class AcceptInvitationCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyInvitationRepository _invitationRepository = Substitute.For<IStudyInvitationRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly AcceptInvitationCommandHandler _handler;

    public AcceptInvitationCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new AcceptInvitationCommandHandler(
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
    public async Task Handle_WithValidToken_ShouldAcceptInvitation()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        invitation.Status.Should().Be(InvitationStatus.Accepted);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldAddUserToStudy()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation(role: StudyRole.Editor);
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(new UserId(2)).Should().BeTrue();
        study.GetMember(new UserId(2))!.Role.Should().Be(StudyRole.Editor);
    }

    [Fact]
    public async Task Handle_WithViewerRole_ShouldAddUserAsViewer()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation(role: StudyRole.Viewer);
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(new UserId(2))!.Role.Should().Be(StudyRole.Viewer);
    }

    [Fact]
    public async Task Handle_Success_ShouldPersistChanges()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new AcceptInvitationCommand("valid-token", "invalid");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_InvitationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new AcceptInvitationCommand("unknown-token", "U00000002");

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
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var invitation = CreateTestInvitation();
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_InvitationAlreadyAccepted_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        invitation.Accept(); // Already accepted
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
    }

    [Fact]
    public async Task Handle_WithExpiredInvitation_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateExpiredInvitation();
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationExpired");
    }

    [Fact]
    public async Task Handle_WithDeclinedInvitation_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        invitation.Decline(); // Already declined
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
    }

    [Fact]
    public async Task Handle_WithCancelledInvitation_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        invitation.Cancel(); // Cancelled
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvitationAlreadyResponded");
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyMember_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var existingMemberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(existingMemberId, StudyRole.Editor, ownerId); // User already a member

        var invitation = CreateTestInvitation();
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002"); // Same user

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("UserAlreadyMember");
    }

    [Fact]
    public async Task Handle_Success_ShouldSetInvitationStatusToAccepted()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithAdminRole_ShouldAddUserAsAdmin()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation(role: StudyRole.Admin);
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(new UserId(2))!.Role.Should().Be(StudyRole.Admin);
    }

    [Fact]
    public async Task Handle_ShouldReturnStudyDto()
    {
        // Arrange
        var study = CreateTestStudy();
        var invitation = CreateTestInvitation();
        var command = new AcceptInvitationCommand(invitation.Token, "U00000002");

        _invitationRepository
            .GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(invitation);

        _studyRepository
            .GetByIdWithMembersAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBeNullOrEmpty();
        result.Value.Title.Should().Be("Test Study");
    }

    #endregion

    #region Helper Methods - Extended

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
}
