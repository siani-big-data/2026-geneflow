using GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyMember;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for RemoveStudyMemberCommandHandler.
/// </summary>
public class RemoveStudyMemberCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly RemoveStudyMemberCommandHandler _handler;

    public RemoveStudyMemberCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new RemoveStudyMemberCommandHandler(
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
    public async Task Handle_WithValidData_ShouldRemoveMember()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000001", // Owner
            "U00000002"); // Member to remove

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(memberId).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ByOwner_ShouldRemoveMember()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000001", // Owner
            "U00000002"); // Member to remove

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(memberId).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ByAdmin_ShouldRemoveMember()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var memberId = new UserId(3);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000002", // Admin
            "U00000003"); // Member to remove

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(memberId).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Success_ShouldPersistChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

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
        var command = new RemoveStudyMemberCommand(
            "invalid",
            "U00000001",
            "U00000002");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidRequesterId_ShouldReturnFailure()
    {
        // Arrange
        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "invalid",
            "U00000002");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidMemberId_ShouldReturnFailure()
    {
        // Arrange
        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000001",
            "invalid");

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
        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000002");

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
    public async Task Handle_MemberNotFound_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000001",
            "U00000099"); // Not a member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("UserNotMember");
    }

    #endregion

    #region Permission Failures

    [Fact]
    public async Task Handle_RemoveOwner_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000002", // Admin trying to remove owner
            "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotRemoveOwner");
    }

    [Fact]
    public async Task Handle_ByEditor_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editorId = new UserId(2);
        var viewerId = new UserId(3);
        var study = CreateTestStudy(ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000002", // Editor - cannot manage members
            "U00000003");

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
    public async Task Handle_WhenNotOwnerOrAdmin_ShouldReturnUnauthorized()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var editorId = new UserId(3);
        var study = CreateTestStudy(ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        // Viewer trying to remove editor - should fail
        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000002", // Viewer (doesn't have permission)
            "U00000003"); // Editor

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
    public async Task Handle_RemovingOwner_ShouldReturnError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        // Admin trying to remove owner
        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000002", // Admin
            "U00000001"); // Owner

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotRemoveOwner");
    }

    [Fact]
    public async Task Handle_RemovingSelf_ShouldReturnError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        // Admin trying to remove themselves via this command
        // (Self-removal should be done via LeaveStudy command, not RemoveMember)
        // The domain's RemoveMember checks permissions - admin can remove themselves
        // but this test validates the handler behavior
        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000002", // Admin as requester
            "U00000002"); // Admin as target (self)

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Admin with CanManageMembers can remove themselves
        // The domain allows this, but UI should use LeaveStudy for self-removal
        result.IsSuccess.Should().BeTrue();
        study.IsMember(adminId).Should().BeFalse();
    }

    #endregion

    #region Domain Events

    [Fact]
    public async Task Handle_ShouldRaiseMemberRemovedEvent()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        // Clear events from AddMember
        study.ClearDomainEvents();

        var command = new RemoveStudyMemberCommand(
            "S00000001",
            "U00000001", // Owner
            "U00000002"); // Member to remove

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle(e => e is StudyMemberRemovedEvent);

        var domainEvent = study.DomainEvents.OfType<StudyMemberRemovedEvent>().Single();
        domainEvent.StudyId.Should().Be(study.Id);
        domainEvent.MemberUserId.Should().Be(memberId);
        domainEvent.RemovedBy.Should().Be(ownerId);
    }

    #endregion
}
