using GeneFlow.ApiNet2.Application.Studies.Commands.LeaveStudy;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for LeaveStudyCommandHandler.
/// </summary>
public class LeaveStudyCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly LeaveStudyCommandHandler _handler;

    public LeaveStudyCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new LeaveStudyCommandHandler(
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
    public async Task Handle_ByEditor_ShouldLeaveStudy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editorId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        var command = new LeaveStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(editorId).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ByViewer_ShouldLeaveStudy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var command = new LeaveStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(viewerId).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ByAdmin_ShouldLeaveStudy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new LeaveStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(adminId).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Success_ShouldPersistChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new LeaveStudyCommand("S00000001", "U00000002");

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
        var command = new LeaveStudyCommand("invalid", "U00000002");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new LeaveStudyCommand("S00000001", "invalid");

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
        var command = new LeaveStudyCommand("S00000001", "U00000002");

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
    public async Task Handle_NotAMember_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new LeaveStudyCommand("S00000001", "U00000099"); // Not a member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotMember");
    }

    #endregion

    #region Permission Failures

    [Fact]
    public async Task Handle_ByOwner_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new LeaveStudyCommand("S00000001", "U00000001"); // Owner

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("OwnerCannotLeave");
    }

    #endregion

    #region Domain Events

    [Fact]
    public async Task Handle_Success_ShouldRaiseMemberRemovedEvent()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editorId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);
        study.ClearDomainEvents();

        var command = new LeaveStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle(e => e is StudyMemberRemovedEvent);
        var evt = study.DomainEvents.OfType<StudyMemberRemovedEvent>().First();
        evt.StudyId.Should().Be(study.Id);
        evt.MemberUserId.Should().Be(editorId);
        evt.RemovedBy.Should().Be(editorId); // User removed themselves
    }

    [Fact]
    public async Task Handle_Success_ShouldRemoveMemberFromStudy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Editor, ownerId);

        var command = new LeaveStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Verify member exists before
        study.IsMember(memberId).Should().BeTrue();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(memberId).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MultipleMembersLeave_ShouldOnlyRemoveRequestingMember()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editor1Id = new UserId(2);
        var editor2Id = new UserId(3);
        var study = CreateTestStudy(ownerId);
        study.AddMember(editor1Id, StudyRole.Editor, ownerId);
        study.AddMember(editor2Id, StudyRole.Editor, ownerId);

        var command = new LeaveStudyCommand("S00000001", "U00000002"); // editor1

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.IsMember(editor1Id).Should().BeFalse();
        study.IsMember(editor2Id).Should().BeTrue();
        study.IsMember(ownerId).Should().BeTrue();
    }

    #endregion
}
