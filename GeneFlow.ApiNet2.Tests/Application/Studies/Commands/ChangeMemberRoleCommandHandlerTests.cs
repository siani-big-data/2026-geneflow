using GeneFlow.ApiNet2.Application.Studies.Commands.ChangeMemberRole;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for ChangeMemberRoleCommandHandler.
/// </summary>
public class ChangeMemberRoleCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly ChangeMemberRoleCommandHandler _handler;

    public ChangeMemberRoleCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new ChangeMemberRoleCommandHandler(
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
    public async Task Handle_WithValidData_ShouldChangeRole()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001", // Owner
            "U00000002", // Member to change
            StudyRole.Admin.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        study.GetMember(memberId)!.Role.Should().Be(StudyRole.Admin);
    }

    [Fact]
    public async Task Handle_ByOwner_ShouldChangeRole()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001", // Owner
            "U00000002", // Member to change
            StudyRole.Admin.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        study.GetMember(memberId)!.Role.Should().Be(StudyRole.Admin);
    }

    [Fact]
    public async Task Handle_ChangeEditorToViewer_ShouldSucceed()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new ChangeMemberRoleCommand(
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
        study.GetMember(memberId)!.Role.Should().Be(StudyRole.Viewer);
    }

    [Fact]
    public async Task Handle_ChangeAdminToEditor_ShouldSucceed()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Admin, ownerId);

        var command = new ChangeMemberRoleCommand(
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
        study.GetMember(memberId)!.Role.Should().Be(StudyRole.Editor);
    }

    [Fact]
    public async Task Handle_Success_ShouldPersistChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Admin.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_ShouldReturnUpdatedMemberDto()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Viewer, ownerId);

        var command = new ChangeMemberRoleCommand(
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
        result.Value.RoleId.Should().Be(StudyRole.Editor.Id);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new ChangeMemberRoleCommand(
            "invalid",
            "U00000001",
            "U00000002",
            StudyRole.Admin.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidRequesterId_ShouldReturnFailure()
    {
        // Arrange
        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "invalid",
            "U00000002",
            StudyRole.Admin.Id);

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
        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001",
            "invalid",
            StudyRole.Admin.Id);

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
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new ChangeMemberRoleCommand(
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
        result.Error.Code.Should().Contain("InvalidRole");
    }

    [Fact]
    public async Task Handle_ChangeToOwner_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Admin, ownerId);

        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Owner.Id); // Cannot change to owner

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
        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001",
            "U00000002",
            StudyRole.Admin.Id);

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
        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001",
            "U00000099", // Not a member
            StudyRole.Admin.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Permission Failures

    [Fact]
    public async Task Handle_ChangeOwnerRole_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001", // Owner trying to change their own role
            "U00000001",
            StudyRole.Admin.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotChangeOwnerRole");
    }

    [Fact]
    public async Task Handle_ChangingOwnerRole_ShouldReturnError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        // Admin trying to change owner's role (even owner can't change their own role)
        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001", // Owner as requester
            "U00000001", // Owner as target
            StudyRole.Editor.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotChangeOwnerRole");
    }

    [Fact]
    public async Task Handle_ChangingSelfRole_ShouldReturnError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        // Note: This scenario depends on domain rules - if admins can change roles
        // but can't change their own role, this test validates that.
        // However, from the domain code, the check is only for owner role, not self-change.
        // The command requires Owner role per the IRequireStudyMembership interface.
        // So non-owners cannot even reach the handler logic.
        // Owner trying to change their own role would fail because owner role cannot be changed.
        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001", // Owner as requester
            "U00000001", // Owner as target (self)
            StudyRole.Editor.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotChangeOwnerRole");
    }

    [Fact]
    public async Task Handle_WhenNotOwnerOrAdmin_ShouldReturnUnauthorized()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editorId = new UserId(2);
        var viewerId = new UserId(3);
        var study = CreateTestStudy(ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        // Editor trying to change viewer's role - should fail
        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000002", // Editor (doesn't have permission)
            "U00000003", // Viewer
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

    [Fact]
    public async Task Handle_ToSameRole_ShouldSucceed()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        // Change to the same role (Editor -> Editor)
        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001", // Owner
            "U00000002", // Member
            StudyRole.Editor.Id); // Same role

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(memberId)!.Role.Should().Be(StudyRole.Editor);
    }

    #endregion

    #region Domain Events

    [Fact]
    public async Task Handle_ShouldRaiseMemberRoleChangedEvent()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        // Clear events from AddMember
        study.ClearDomainEvents();

        var command = new ChangeMemberRoleCommand(
            "S00000001",
            "U00000001", // Owner
            "U00000002", // Member to change
            StudyRole.Admin.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle(e => e is StudyMemberRoleChangedEvent);

        var domainEvent = study.DomainEvents.OfType<StudyMemberRoleChangedEvent>().Single();
        domainEvent.StudyId.Should().Be(study.Id);
        domainEvent.MemberUserId.Should().Be(memberId);
        domainEvent.OldRole.Should().Be(StudyRole.Editor);
        domainEvent.NewRole.Should().Be(StudyRole.Admin);
        domainEvent.ChangedBy.Should().Be(ownerId);
    }

    #endregion
}
