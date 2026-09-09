using GeneFlow.ApiNet2.Application.Studies.Commands.UpdateReadme;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for UpdateReadmeCommandHandler.
/// </summary>
public class UpdateReadmeCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly UpdateReadmeCommandHandler _handler;

    public UpdateReadmeCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new UpdateReadmeCommandHandler(_studyRepository, _unitOfWork);
    }

    private static Study CreateTestStudy(UserId? ownerId = null)
    {
        var studyId = new StudyId(1);
        var owner = ownerId ?? new UserId(1);
        var title = StudyTitle.Create("Readme Study").Value;
        var description = StudyDescription.Create("desc").Value;
        return Study.Create(studyId, owner, title, description, ResearchField.Genomics).Value;
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateReadmeAndReturnDto()
    {
        // Arrange
        var study = CreateTestStudy();
        const string markdown = "# Title\n\nbody";
        var command = new UpdateReadmeCommand("S00000001", "U00000001", markdown);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReadmeMarkdown.Should().Be(markdown);
        study.ReadmeMarkdown.Should().Be(markdown);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullMarkdown_ShouldClearReadme()
    {
        // Arrange
        var owner = new UserId(1);
        var study = CreateTestStudy(owner);
        study.UpdateReadme("existing", owner);
        var command = new UpdateReadmeCommand("S00000001", "U00000001", null);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        study.ReadmeMarkdown.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateReadmeCommand("invalid-id", "U00000001", "# hi");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateReadmeCommand("S00000001", "invalid-user", "# hi");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdateReadmeCommand("S00000001", "U00000001", "# hi");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonMemberUser_ShouldReturnInsufficientPermissions()
    {
        // Arrange
        var owner = new UserId(1);
        var study = CreateTestStudy(owner);
        var command = new UpdateReadmeCommand("S00000001", "U00000099", "# hi");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(StudyErrors.InsufficientPermissions);
        study.ReadmeMarkdown.Should().BeNull();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MarkdownTooLong_ShouldReturnFailure()
    {
        // Arrange
        var owner = new UserId(1);
        var study = CreateTestStudy(owner);
        var tooLong = new string('x', Study.MaxReadmeLength + 1);
        var command = new UpdateReadmeCommand("S00000001", "U00000001", tooLong);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Study.ReadmeTooLong");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
