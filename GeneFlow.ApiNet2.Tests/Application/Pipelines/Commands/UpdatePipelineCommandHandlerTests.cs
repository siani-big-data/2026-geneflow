using GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Commands;

/// <summary>
/// Unit tests for UpdatePipelineCommandHandler.
/// </summary>
public class UpdatePipelineCommandHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly UpdatePipelineCommandHandler _handler;

    // Test data
    private const string ValidUserId = "U00000001";
    private const string ValidStudyId = "S00000001";
    private const string ValidPipelineId = "P00000001";
    private const string UpdatedName = "Updated Pipeline Name";
    private const string UpdatedDescription = "Updated description";

    public UpdatePipelineCommandHandlerTests()
    {
        _pipelineRepository = Substitute.For<IPipelineRepository>();
        _unitOfWork = Substitute.For<IPipelineUnitOfWork>();

        _handler = new UpdatePipelineCommandHandler(
            _pipelineRepository,
            _unitOfWork);
    }

    #region Helper Methods

    private static Pipeline CreateDraftPipeline(UserId ownerId)
    {
        var pipelineId = new PipelineId(1);
        var studyId = new StudyId(1);
        var name = PipelineName.Create("Original Name").Value;
        var description = PipelineDescription.Create("Original description").Value;

        return Pipeline.Create(pipelineId, studyId, ownerId, name, description).Value;
    }

    private static Pipeline CreateActivePipeline(UserId ownerId)
    {
        var pipeline = CreateDraftPipeline(ownerId);
        var config = StepConfiguration.Create("{}", StepType.Quality).Value;
        pipeline.AddStep(StepType.Quality, config, "Quality Step", true, ownerId);
        pipeline.Activate(ownerId);
        return pipeline;
    }

    private static Pipeline CreateArchivedPipeline(UserId ownerId)
    {
        var pipeline = CreateDraftPipeline(ownerId);
        pipeline.Archive(ownerId);
        return pipeline;
    }

    #endregion

    #region Handle - Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnSuccessWithUpdatedPipelineDto()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            UpdatedDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<PipelineDto>();
        result.Value.Name.Should().Be(UpdatedName);
        result.Value.Description.Should().Be(UpdatedDescription);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldPersistChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            UpdatedDescription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldSucceed()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    #endregion

    #region Handle - Pipeline Not Found

    [Fact]
    public async Task Handle_WhenPipelineNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            UpdatedDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.NotFound);
    }

    #endregion

    #region Handle - Pipeline Not Editable

    [Fact]
    public async Task Handle_WhenPipelineIsActive_ShouldReturnNotEditableError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            UpdatedDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotEditable");
    }

    [Fact]
    public async Task Handle_WhenPipelineIsArchived_ShouldReturnNotEditableError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateArchivedPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            UpdatedDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotEditable");
    }

    #endregion

    #region Handle - Validation Failures

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithInvalidName_ShouldReturnValidationError(string invalidName)
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            invalidName,
            UpdatedDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }

    [Fact]
    public async Task Handle_WithTooLongDescription_ShouldReturnValidationError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);
        var tooLongDescription = new string('x', 1001); // Assuming max 1000 chars

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            tooLongDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Description");
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdatePipelineCommand(
            "invalid-user-id",
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            UpdatedDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.InvalidUserId);
    }

    [Fact]
    public async Task Handle_WithInvalidPipelineId_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            "invalid-pipeline-id",
            UpdatedName,
            UpdatedDescription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.NotFound);
    }

    #endregion

    #region Handle - SaveChanges Verification

    [Fact]
    public async Task Handle_WhenPipelineNotFound_ShouldNotCallSaveChanges()
    {
        // Arrange
        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            UpdatedDescription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPipelineNotEditable_ShouldNotCallSaveChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            UpdatedName,
            UpdatedDescription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidationError_ShouldNotCallSaveChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new UpdatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            "", // Invalid name
            UpdatedDescription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
