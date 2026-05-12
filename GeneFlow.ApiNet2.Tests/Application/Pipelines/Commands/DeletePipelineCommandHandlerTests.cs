using GeneFlow.ApiNet2.Application.Pipelines.Commands.DeletePipeline;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Commands;

/// <summary>
/// Unit tests for DeletePipelineCommandHandler.
/// </summary>
public class DeletePipelineCommandHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly DeletePipelineCommandHandler _handler;

    // Test data
    private const string ValidUserId = "U00000001";
    private const string ValidStudyId = "S00000001";
    private const string ValidPipelineId = "P00000001";

    public DeletePipelineCommandHandlerTests()
    {
        _pipelineRepository = Substitute.For<IPipelineRepository>();
        _unitOfWork = Substitute.For<IPipelineUnitOfWork>();

        _handler = new DeletePipelineCommandHandler(
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
    public async Task Handle_WithDraftPipeline_ShouldReturnSuccess()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeletePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithActivePipeline_ShouldReturnSuccess()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);

        _pipelineRepository
            .GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeletePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidPipeline_ShouldDeleteAndPersist()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeletePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _pipelineRepository.Received(1).Delete(pipeline);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Pipeline Not Found

    [Fact]
    public async Task Handle_WhenPipelineNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _pipelineRepository
            .GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        var command = new DeletePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.NotFound);
    }

    #endregion

    #region Handle - Pipeline Not Deletable

    [Fact]
    public async Task Handle_WhenPipelineIsArchived_ShouldReturnNotDeletableError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateArchivedPipeline(ownerId);

        _pipelineRepository
            .GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeletePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.PipelineNotDeletable);
    }

    [Fact]
    public async Task Handle_WhenPipelineNotDeletable_ShouldNotCallDelete()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateArchivedPipeline(ownerId);

        _pipelineRepository
            .GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeletePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _pipelineRepository.DidNotReceive().Delete(Arg.Any<Pipeline>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeletePipelineCommand(
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
        var command = new DeletePipelineCommand(
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

    #region Handle - Repository Interaction

    [Fact]
    public async Task Handle_WithValidPipeline_ShouldCallRepositoryDelete()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);

        _pipelineRepository
            .GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        var command = new DeletePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _pipelineRepository.Received(1).Delete(pipeline);
    }

    [Fact]
    public async Task Handle_WhenPipelineNotFound_ShouldNotCallDelete()
    {
        // Arrange
        _pipelineRepository
            .GetByIdAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        var command = new DeletePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _pipelineRepository.DidNotReceive().Delete(Arg.Any<Pipeline>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
