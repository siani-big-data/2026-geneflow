using GeneFlow.ApiNet2.Application.Studies.Commands.TransferOwnership;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for TransferOwnershipCommandHandler.
/// </summary>
public class TransferOwnershipCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly TransferOwnershipCommandHandler _handler;

    public TransferOwnershipCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new TransferOwnershipCommandHandler(
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
    public async Task Handle_ToAdmin_ShouldTransferOwnership()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000001", // Current owner
            "U00000002"); // New owner (admin)

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        study.OwnerId.Should().Be(adminId);
    }

    [Fact]
    public async Task Handle_Success_OldOwnerBecomesAdmin()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000001",
            "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.GetMember(ownerId)!.Role.Should().Be(StudyRole.Admin);
    }

    [Fact]
    public async Task Handle_Success_ShouldPersistChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new TransferOwnershipCommand(
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

    [Fact]
    public async Task Handle_Success_ReturnsUpdatedStudyDto()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000001",
            "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Test Study");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new TransferOwnershipCommand(
            "invalid",
            "U00000001",
            "U00000002");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidCurrentOwnerId_ShouldReturnFailure()
    {
        // Arrange
        var command = new TransferOwnershipCommand(
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
    public async Task Handle_WithInvalidNewOwnerId_ShouldReturnFailure()
    {
        // Arrange
        var command = new TransferOwnershipCommand(
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
        var command = new TransferOwnershipCommand(
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
    public async Task Handle_NewOwnerNotMember_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new TransferOwnershipCommand(
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

    [Fact]
    public async Task Handle_NewOwnerNotAdmin_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var editorId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(editorId, StudyRole.Editor, ownerId);

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000001",
            "U00000002"); // Editor, not admin

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NewOwnerMustBeAdmin");
    }

    #endregion

    #region Permission Failures

    [Fact]
    public async Task Handle_ByNonOwner_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000002", // Admin trying to transfer (not owner)
            "U00000003");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("OnlyOwnerCanTransfer");
    }

    [Fact]
    public async Task Handle_ToSelf_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000001", // Current owner
            "U00000001"); // Same as current owner (transfer to self)

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Owner has Owner role, not Admin role, so transfer fails
        result.Error.Code.Should().Contain("NewOwnerMustBeAdmin");
    }

    #endregion

    #region Domain Events

    [Fact]
    public async Task Handle_Success_ShouldRaiseOwnershipTransferredEvent()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);
        study.ClearDomainEvents();

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000001",
            "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.DomainEvents.Should().ContainSingle(e => e is StudyOwnershipTransferredEvent);
        var evt = study.DomainEvents.OfType<StudyOwnershipTransferredEvent>().First();
        evt.StudyId.Should().Be(study.Id);
        evt.PreviousOwnerId.Should().Be(ownerId);
        evt.NewOwnerId.Should().Be(adminId);
    }

    [Fact]
    public async Task Handle_Success_ShouldUpdateNewOwnerRole()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000001",
            "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var newOwnerMember = study.GetMember(adminId);
        newOwnerMember.Should().NotBeNull();
        newOwnerMember!.Role.Should().Be(StudyRole.Owner);
    }

    [Fact]
    public async Task Handle_ToViewer_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var command = new TransferOwnershipCommand(
            "S00000001",
            "U00000001",
            "U00000002"); // Viewer, not admin

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NewOwnerMustBeAdmin");
    }

    #endregion
}
