using GeneFlow.ApiNet2.Application.Studies.Commands.DuplicateStudy;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for DuplicateStudyCommandHandler.
/// </summary>
public class DuplicateStudyCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly DuplicateStudyCommandHandler _handler;

    public DuplicateStudyCommandHandlerTests()
    {
        _sequenceGenerator
            .NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(2L)); // New study ID

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new DuplicateStudyCommandHandler(
            _studyRepository,
            _unitOfWork,
            _sequenceGenerator);
    }

    #region Helper Methods

    private static Study CreateTestStudy(
        UserId? ownerId = null,
        string title = "Original Study",
        string? institution = null,
        string? principalInvestigator = null)
    {
        var studyId = new StudyId(1);
        var userId = ownerId ?? new UserId(1);
        var titleVo = StudyTitle.Create(title).Value;
        var description = StudyDescription.Create("Test description").Value;

        var study = Study.Create(studyId, userId, titleVo, description, ResearchField.Genomics).Value;

        if (!string.IsNullOrEmpty(institution))
            study.UpdateInstitution(institution, userId);

        if (!string.IsNullOrEmpty(principalInvestigator))
            study.UpdatePrincipalInvestigator(principalInvestigator, userId);

        return study;
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldDuplicateStudy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId, "My Research Study");

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("My Research Study (Copy)");
        result.Value.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Handle_ShouldCopyDescription()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Test description");
    }

    [Fact]
    public async Task Handle_ShouldCopyResearchField()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResearchField.Should().Be("Genomics");
    }

    [Fact]
    public async Task Handle_ShouldCopyInstitution()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId, institution: "MIT");

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Institution.Should().Be("MIT");
    }

    [Fact]
    public async Task Handle_ShouldCopyPrincipalInvestigator()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId, principalInvestigator: "Dr. Jane Smith");

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrincipalInvestigator.Should().Be("Dr. Jane Smith");
    }

    [Fact]
    public async Task Handle_ShouldCopyTags()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        study.AddTag("genomics", ownerId);
        study.AddTag("cancer", ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Should().HaveCount(2);
        result.Value.Tags.Should().Contain(new[] { "genomics", "cancer" });
    }

    [Fact]
    public async Task Handle_AsMember_ShouldDuplicateStudy()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Viewer, ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NewStudy_ShouldHaveUserAsOwner()
    {
        // Arrange
        var ownerId = new UserId(1);
        var memberId = new UserId(2);
        var study = CreateTestStudy(ownerId);
        study.AddMember(memberId, StudyRole.Viewer, ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000002");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Should().HaveCount(1);
        result.Value.Members.First().UserId.Should().Be("U00000002");
        result.Value.Members.First().Role.Should().Be("Owner");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new DuplicateStudyCommand("S00000001", "invalid-user");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new DuplicateStudyCommand("invalid-id", "U00000001");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_StudyNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns((Study?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Permission Failures

    [Fact]
    public async Task Handle_AsNonMember_ShouldReturnFailure()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000099"); // Non-member

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldGenerateNewSequenceId()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(
            StudyId.SequenceName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAddNewStudyToRepository()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).AddAsync(
            Arg.Any<Study>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCopySettings()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        var customSettings = StudySettings.Create(
            allowPublicComments: true,
            allowDataDownload: false,
            requireApprovalToJoin: true);
        study.UpdateSettings(customSettings, ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        Study? capturedStudy = null;
        await _studyRepository.AddAsync(
            Arg.Do<Study>(s => capturedStudy = s),
            Arg.Any<CancellationToken>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedStudy.Should().NotBeNull();
        capturedStudy!.Settings.AllowPublicComments.Should().Be(true);
        capturedStudy.Settings.AllowDataDownload.Should().Be(false);
        capturedStudy.Settings.RequireApprovalToJoin.Should().Be(true);
    }

    [Fact]
    public async Task Handle_ShouldRaiseStudyCreatedEvent()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        Study? capturedStudy = null;
        await _studyRepository.AddAsync(
            Arg.Do<Study>(s => capturedStudy = s),
            Arg.Any<CancellationToken>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedStudy.Should().NotBeNull();
        capturedStudy!.DomainEvents.Should().ContainSingle(e => e is StudyCreatedEvent);
        var evt = capturedStudy.DomainEvents.OfType<StudyCreatedEvent>().First();
        evt.StudyId.Should().Be(capturedStudy.Id);
        evt.OwnerId.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_DuplicatedStudy_ShouldHaveDraftStatus()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);
        // Change original study status to Active
        study.ChangeStatus(StudyStatus.Active, ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Draft");
        result.Value.StatusId.Should().Be(StudyStatus.Draft.Id);
    }

    [Fact]
    public async Task Handle_DuplicatedStudy_ShouldNotBeFeatured()
    {
        // Arrange
        var ownerId = new UserId(1);
        var study = CreateTestStudy(ownerId);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithLongTitle_ShouldTruncateAndAddCopySuffix()
    {
        // Arrange
        var ownerId = new UserId(1);
        var longTitle = new string('A', 95); // Near max length
        var study = CreateTestStudy(ownerId, longTitle);

        var command = new DuplicateStudyCommand("S00000001", "U00000001");

        _studyRepository
            .GetByIdAsync(Arg.Any<StudyId>(), Arg.Any<CancellationToken>())
            .Returns(study);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().EndWith("(Copy)");
        result.Value.Title.Length.Should().BeLessOrEqualTo(100);
    }

    #endregion
}
