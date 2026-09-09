using GeneFlow.ApiNet2.Application.Pipelines.Commands.CreatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Commands;

/// <summary>
/// Unit tests for CreatePipelineCommandHandler.
/// </summary>
public class CreatePipelineCommandHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly CreatePipelineCommandHandler _handler;

    // Test data
    private const string ValidUserId = "U00000001";
    private const string ValidStudyId = "S00000001";
    private const string ValidName = "Quality Analysis Pipeline";
    private const string ValidDescription = "A pipeline for quality analysis";

    public CreatePipelineCommandHandlerTests()
    {
        _pipelineRepository = Substitute.For<IPipelineRepository>();
        _unitOfWork = Substitute.For<IPipelineUnitOfWork>();
        _sequenceGenerator = Substitute.For<ISequenceGenerator>();

        _handler = new CreatePipelineCommandHandler(
            _pipelineRepository,
            _unitOfWork,
            _sequenceGenerator);

        // Default setup - sequence generator returns a valid ID
        _sequenceGenerator
            .NextAsync(PipelineId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        // Default setup - no duplicate names
        _pipelineRepository
            .NameExistsInStudyAsync(Arg.Any<StudyId>(), Arg.Any<string>(), Arg.Any<PipelineId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    #region Handle - Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnSuccessWithPipelineDto()
    {
        // Arrange
        var command = new CreatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidName,
            ValidDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<PipelineDto>();
        result.Value.Name.Should().Be(ValidName);
        result.Value.Description.Should().Be(ValidDescription);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldSetStatusToDraft()
    {
        // Arrange
        var command = new CreatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidName,
            ValidDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StatusId.Should().Be(PipelineStatus.Draft.Id);
        result.Value.StatusName.Should().Be(PipelineStatus.Draft.DisplayName);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldPersistPipeline()
    {
        // Arrange
        var command = new CreatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidName,
            ValidDescription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _pipelineRepository.Received(1).AddAsync(
            Arg.Is<Pipeline>(p => p.Name.Value == ValidName),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldSucceed()
    {
        // Arrange
        var command = new CreatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidName,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldGenerateSequentialId()
    {
        // Arrange
        var command = new CreatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidName,
            ValidDescription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(
            PipelineId.SequenceName,
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Validation Failures

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        _pipelineRepository
            .NameExistsInStudyAsync(Arg.Any<StudyId>(), ValidName, Arg.Any<PipelineId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidName,
            ValidDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithInvalidName_ShouldReturnFailure(string invalidName)
    {
        // Arrange
        var command = new CreatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            invalidName,
            ValidDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreatePipelineCommand(
            "invalid-user-id",
            ValidStudyId,
            ValidName,
            ValidDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.InvalidUserId);
    }

    [Fact]
    public async Task Handle_WithInvalidStudyId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreatePipelineCommand(
            ValidUserId,
            "invalid-study-id",
            ValidName,
            ValidDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.InvalidStudyId);
    }

    #endregion
}
