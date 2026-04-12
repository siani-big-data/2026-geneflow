using GeneFlow.ApiNet2.Application.Studies.Commands.CreateStudy;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Studies.Commands;

/// <summary>
/// Unit tests for CreateStudyCommandHandler.
/// </summary>
public class CreateStudyCommandHandlerTests
{
    private readonly IStudyRepository _studyRepository = Substitute.For<IStudyRepository>();
    private readonly IStudyUnitOfWork _unitOfWork = Substitute.For<IStudyUnitOfWork>();
    private readonly ISequenceGenerator _sequenceGenerator = Substitute.For<ISequenceGenerator>();
    private readonly CreateStudyCommandHandler _handler;

    public CreateStudyCommandHandlerTests()
    {
        _sequenceGenerator
            .NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(1L));

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new CreateStudyCommandHandler(
            _studyRepository,
            _unitOfWork,
            _sequenceGenerator);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateStudy()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Genomic Analysis Study",
            "Study description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Genomic Analysis Study");
        result.Value.Description.Should().Be("Study description");
        result.Value.ResearchField.Should().Be("Genomics");
        result.Value.Status.Should().Be("Draft");

        await _studyRepository.Received(1).AddAsync(Arg.Any<Study>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutDescription_ShouldCreateStudy()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Study Without Description",
            null, // No description
            ResearchField.Proteomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Study Without Description");
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithInstitution_ShouldSetInstitution()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Institutional Study",
            "Description",
            ResearchField.Genomics.Id,
            Institution: "MIT");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Institution.Should().Be("MIT");
    }

    [Fact]
    public async Task Handle_WithPrincipalInvestigator_ShouldSetPI()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "PI Study",
            "Description",
            ResearchField.Genomics.Id,
            PrincipalInvestigator: "Dr. Jane Smith");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrincipalInvestigator.Should().Be("Dr. Jane Smith");
    }

    [Fact]
    public async Task Handle_WithTags_ShouldAddTags()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Tagged Study",
            "Description",
            ResearchField.Genomics.Id,
            Tags: new[] { "cancer", "BRCA1", "mutation" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Tags.Should().HaveCount(3);
        result.Value.Tags.Should().Contain(new[] { "cancer", "brca1", "mutation" }); // normalized to lowercase
    }

    [Fact]
    public async Task Handle_WithAllFields_ShouldCreateCompleteStudy()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Complete Study",
            "Full description of the study",
            ResearchField.Genetics.Id,
            Institution: "Stanford University",
            PrincipalInvestigator: "Dr. John Doe",
            Tags: new[] { "clinical", "rare-disease" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Complete Study");
        result.Value.Description.Should().Be("Full description of the study");
        result.Value.ResearchField.Should().Be("Genetics");
        result.Value.Institution.Should().Be("Stanford University");
        result.Value.PrincipalInvestigator.Should().Be("Dr. John Doe");
        result.Value.Tags.Should().HaveCount(2);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "invalid-user-id",
            "Test Study",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidResearchField_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Test Study",
            "Description",
            999); // Invalid research field ID

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidResearchField");
    }

    [Fact]
    public async Task Handle_WithEmptyTitle_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "", // Empty title
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithTooShortTitle_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "AB", // Too short (min 5 chars)
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithTooLongTitle_ShouldReturnFailure()
    {
        // Arrange
        var longTitle = new string('A', 201); // Max 200 chars
        var command = new CreateStudyCommand(
            "U00000001",
            longTitle,
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithTooLongDescription_ShouldReturnFailure()
    {
        // Arrange
        var longDescription = new string('A', 5001); // Max 5000 chars
        var command = new CreateStudyCommand(
            "U00000001",
            "Test Study",
            longDescription,
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Interactions

    [Fact]
    public async Task Handle_ShouldGenerateSequenceId()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Test Study",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(
            StudyId.SequenceName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPersistStudy()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Test Study",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _studyRepository.Received(1).AddAsync(
            Arg.Is<Study>(s => s.Title.Value == "Test Study"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallUnitOfWorkSaveChanges()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Test Study",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Study Properties

    [Fact]
    public async Task Handle_NewStudy_ShouldHaveDraftStatus()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Draft Study",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.StatusId.Should().Be(StudyStatus.Draft.Id);
        result.Value.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Handle_NewStudy_ShouldNotBeFeatured()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Non-featured Study",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NewStudy_ShouldHaveOwnerAsMember()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Study With Owner",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.Members.Should().HaveCount(1);
        result.Value.Members.First().UserId.Should().Be("U00000001");
        result.Value.Members.First().Role.Should().Be("Owner");
    }

    [Fact]
    public async Task Handle_NewStudy_ShouldHaveZeroMetrics()
    {
        // Arrange
        var command = new CreateStudyCommand(
            "U00000001",
            "Fresh Study",
            "Description",
            ResearchField.Genomics.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.ViewsCount.Should().Be(0);
        result.Value.StarsCount.Should().Be(0);
    }

    #endregion
}
