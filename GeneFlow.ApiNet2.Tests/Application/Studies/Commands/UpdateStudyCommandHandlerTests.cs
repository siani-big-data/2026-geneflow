using GeneFlow.ApiNet2.Application.Studies.Commands.UpdateStudy;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for UpdateStudyCommandHandler.
/// </summary>
public class UpdateStudyCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly UpdateStudyCommandHandler _handler;

    public UpdateStudyCommandHandlerTests()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new UpdateStudyCommandHandler(
            _studyRepository,
            _unitOfWork);
    }

    #region Helper Methods

    private static Study CreateTestStudy(UserId? ownerId = null)
    {
        var studyId = new StudyId(1);
        var userId = ownerId ?? new UserId(1);
        var title = StudyTitle.Create("Original Title").Value;
        var description = StudyDescription.Create("Original description").Value;

        return Study.Create(studyId, userId, title, description, ResearchField.Genomics).Value;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateStudy()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Updated Title",
            "Updated description",
            ResearchField.Proteomics.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Updated Title");
        result.Value.Description.Should().Be("Updated description");
        result.Value.ResearchField.Should().Be("Proteomics");
    }

    [Fact]
    public async Task Handle_WithInstitution_ShouldUpdateInstitution()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Updated Study",
            "Description",
            ResearchField.Genomics.Id,
            Institution: "Harvard University");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Institution.Should().Be("Harvard University");
    }

    [Fact]
    public async Task Handle_WithPrincipalInvestigator_ShouldUpdatePI()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Updated Study",
            "Description",
            ResearchField.Genomics.Id,
            PrincipalInvestigator: "Dr. New PI");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrincipalInvestigator.Should().Be("Dr. New PI");
    }

    [Fact]
    public async Task Handle_WithTags_ShouldReplaceTags()
    {
        // Arrange
        var study = CreateTestStudy();
        study.AddTag("old-tag", study.OwnerId);
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Updated Study",
            "Description",
            ResearchField.Genomics.Id,
            Tags: new[] { "new-tag1", "new-tag2" });

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Should().Contain(new[] { "new-tag1", "new-tag2" });
        result.Value.Tags.Should().NotContain("old-tag");
    }

    [Fact]
    public async Task Handle_ByAdmin_ShouldUpdateStudy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var adminId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(adminId, StudyRole.Admin, ownerId);

        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000002", // Admin
            "Admin Updated Title",
            "Description",
            ResearchField.Genomics.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Admin Updated Title");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateStudyCommand(
            "invalid-id",
            "U00000001",
            "Title",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateStudyCommand(
            "S00000001",
            "invalid-user",
            "Title",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Title",
            "Description",
            ResearchField.Genomics.Id);

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
    public async Task Handle_WithInvalidResearchField_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Title",
            "Description",
            999); // Invalid

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
    public async Task Handle_ByViewer_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var viewerId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(viewerId, StudyRole.Viewer, ownerId);

        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000002", // Viewer
            "Viewer Title",
            "Description",
            ResearchField.Genomics.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ByNonMember_ShouldReturnFailure()
    {
        // Arrange
        var study = CreateTestStudy(new UserId(1));
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000099", // Not a member
            "Non-member Title",
            "Description",
            ResearchField.Genomics.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldFetchStudyById()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Title",
            "Description",
            ResearchField.Genomics.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).GetByIdAsync(
            Arg.Any<StudyId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Success_ShouldCallSaveChanges()
    {
        // Arrange
        var study = CreateTestStudy();
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Title",
            "Description",
            ResearchField.Genomics.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailuREDACTED()
    {
        // Arrange
        var command = new UpdateStudyCommand(
            "S00000001",
            "U00000001",
            "Title",
            "Description",
            ResearchField.Genomics.Id);

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
