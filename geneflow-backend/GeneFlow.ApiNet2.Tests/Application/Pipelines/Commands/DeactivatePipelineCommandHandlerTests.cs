using GeneFlow.ApiNet2.Application.Pipelines.Commands.DeactivatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Commands;

/// <summary>
/// Unit tests for DeactivatePipelineCommandHandler.
/// </summary>
public class DeactivatePipelineCommandHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly DeactivatePipelineCommandHandler _handler;

    // Test data
    private const string ValidUserId = "U00000001";
    private const string ValidStudyId = "S00000001";
    private const string ValidPipelineId = "P00000001";

    public DeactivatePipelineCommandHandlerTests()
    {
        _pipelineRepository = Substitute.For<IPipelineRepository>();
        _unitOfWork = Substitute.For<IPipelineUnitOfWork>();

        _handler = new DeactivatePipelineCommandHandler(
            _pipelineRepository,
            _unitOfWork);
    }

    #region Helper Methods

    private static Pipeline CreateDraftPipeline(UserId ownerId)
    {
        var pipelineId = new PipelineId(1);
        var studyId = new StudyId(1);
        var name = PipelineName.Create("Test Pipeline").Value;
        var description = PipelineDescription.Create("Test description").Value;

        return Pipeline.Create(pipelineId, studyId, ownerId, name, description).Value;
    }

    private static Pipeline CreateDraftPipelineWithSteps(UserId ownerId)
    {
        var pipeline = CreateDraftPipeline(ownerId);
        var config = StepConfiguration.Create("{}", StepType.Quality).Value;
        pipeline.AddStep(StepType.Quality, config, "Quality Step", true, ownerId);
        return pipeline;
    }

    private static Pipeline CreateActivePipeline(UserId ownerId)
    {
        var pipeline = CreateDraftPipelineWithSteps(ownerId);
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
    public async Task Handle_WithActivePipeline_ShouldReturnSuccessWithDeactivatedPipeline()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<PipelineDto>();
        result.Value.StatusId.Should().Be(PipelineStatus.Draft.Id);
        result.Value.StatusName.Should().Be(PipelineStatus.Draft.DisplayName);
    }

    [Fact]
    public async Task Handle_WithValidPipeline_ShouldPersistChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidPipeline_ShouldSetCanBeEditedToTrue()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CanBeEdited.Should().BeTrue();
        result.Value.CanBeExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithValidPipeline_ShouldReturnDraftStatus()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StatusId.Should().Be(PipelineStatus.Draft.Id);
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

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.NotFound);
    }

    [Fact]
    public async Task Handle_WhenPipelineNotFound_ShouldNotCallSaveChanges()
    {
        // Arrange
        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Invalid Status Transition

    [Fact]
    public async Task Handle_WhenPipelineIsDraft_ShouldReturnInvalidTransitionError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public async Task Handle_WhenPipelineIsArchived_ShouldReturnInvalidTransitionError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateArchivedPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public async Task Handle_WhenTransitionInvalid_ShouldNotCallSaveChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeactivatePipelineCommand(
            "invalid-user-id",
            ValidStudyId,
            ValidPipelineId);

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
        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            "invalid-pipeline-id");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.NotFound);
    }

    #endregion

    #region Handle - Domain Event Verification

    [Fact]
    public async Task Handle_WithValidPipeline_ShouldUpdateModifiedTimestamp()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var originalModifiedAt = pipeline.ModifiedAt;

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeactivatePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // The pipeline's ModifiedAt should be updated after deactivation
        pipeline.ModifiedAt.Should().BeOnOrAfter(originalModifiedAt ?? DateTime.MinValue);
    }

    #endregion
}
