using GeneFlow.ApiNet2.Application.Pipelines.Commands.ExecutePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.DTOs;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Pipelines;
using GeneFlow.ApiNet2.Domain.Pipelines.Entities;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Pipelines.Commands;

/// <summary>
/// Unit tests for ExecutePipelineCommandHandler.
/// </summary>
public class ExecutePipelineCommandHandlerTests
{
    private readonly IPipelineRepository _pipelineRepository;
    private readonly IPipelineExecutionRepository _executionRepository;
    private readonly IPipelineUnitOfWork _unitOfWork;
    private readonly ITraceRepository _traceRepository;
    private readonly ISequenceGenerator _sequenceGenerator;
    private readonly IJobPublisher _jobPublisher;
    private readonly ExecutePipelineCommandHandler _handler;

    // Test data
    private const string ValidUserId = "U00000001";
    private const string ValidStudyId = "S00000001";
    private const string ValidPipelineId = "P00000001";
    private static readonly string ValidTraceId = Guid.NewGuid().ToString();

    public ExecutePipelineCommandHandlerTests()
    {
        _pipelineRepository = Substitute.For<IPipelineRepository>();
        _executionRepository = Substitute.For<IPipelineExecutionRepository>();
        _unitOfWork = Substitute.For<IPipelineUnitOfWork>();
        _traceRepository = Substitute.For<ITraceRepository>();
        _sequenceGenerator = Substitute.For<ISequenceGenerator>();
        _jobPublisher = Substitute.For<IJobPublisher>();

        _handler = new ExecutePipelineCommandHandler(
            _pipelineRepository,
            _executionRepository,
            _unitOfWork,
            _traceRepository,
            _sequenceGenerator,
            _jobPublisher);

        // Default setup - sequence generator returns valid IDs
        _sequenceGenerator
            .NextAsync(PipelineExecutionId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        // Default setup - no running executions
        _executionRepository
            .HasRunningExecutionForTraceAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    #region Helper Methods

    private static Pipeline CreateActivePipeline(UserId ownerId)
    {
        var pipelineId = new PipelineId(1);
        var studyId = new StudyId(1);
        var name = PipelineName.Create("Test Pipeline").Value;
        var description = PipelineDescription.Create("Test description").Value;

        var pipeline = Pipeline.Create(pipelineId, studyId, ownerId, name, description).Value;
        var config = StepConfiguration.Create("{}", StepType.Quality).Value;
        pipeline.AddStep(StepType.Quality, config, "Quality Step", true, ownerId);
        pipeline.Activate(ownerId);

        return pipeline;
    }

    private static Pipeline CreateDraftPipeline(UserId ownerId)
    {
        var pipelineId = new PipelineId(1);
        var studyId = new StudyId(1);
        var name = PipelineName.Create("Test Pipeline").Value;
        var description = PipelineDescription.Create("Test description").Value;

        var pipeline = Pipeline.Create(pipelineId, studyId, ownerId, name, description).Value;
        var config = StepConfiguration.Create("{}", StepType.Quality).Value;
        pipeline.AddStep(StepType.Quality, config, "Quality Step", true, ownerId);

        return pipeline;
    }

    private static Trace CreateBaseTrace()
    {
        var traceId = TraceId.New();
        var studyId = new StudyId(1);
        var userId = new UserId(1);
        var name = TraceName.Create("Test Trace").Value;
        var description = TraceDescription.Create("Test description").Value;
        var file = TraceFile.Create("test.ab1", "application/octet-stream", "/traces/test.ab1", 1024, "abc123").Value;
        var format = TraceFormat.AB1;

        return Trace.Create(traceId, studyId, userId, name, description, file, format).Value;
    }

    private static Trace CreateProcessedTrace()
    {
        var trace = CreateBaseTrace();
        trace.StartProcessing();
        trace.TransitionToProcessing();
        var metrics = QualityMetrics.Create(35.5m, 500, 90m, 80m, 480, 50m).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    private static Trace CreateUploadedTrace()
    {
        return CreateBaseTrace();
    }

    private static Trace CreateProcessingTrace()
    {
        var trace = CreateBaseTrace();
        trace.StartProcessing();
        trace.TransitionToProcessing();
        return trace;
    }

    #endregion

    #region Handle - Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnSuccessWithExecutionDto()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<PipelineExecutionDto>();
        result.Value.PipelineId.Should().Be(pipeline.Id.ToString());
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldPersistExecution()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _executionRepository.Received(1).AddAsync(
            Arg.Any<PipelineExecution>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldPublishJobToRedis()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _jobPublisher.Received(1).PublishPipelineJobAsync(
            Arg.Any<PipelineJob>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldGenerateSequentialExecutionId()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _sequenceGenerator.Received(1).NextAsync(
            PipelineExecutionId.SequenceName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnExecutionWithCorrectStepCount()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSteps.Should().Be(pipeline.EnabledStepCount);
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

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.NotFound);
    }

    #endregion

    #region Handle - Pipeline Not Active

    [Fact]
    public async Task Handle_WhenPipelineIsNotActive_ShouldReturnNotExecutableError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateDraftPipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotExecutable");
    }

    #endregion

    #region Handle - Trace Not Processed

    [Fact]
    public async Task Handle_WhenTraceNotFound_ShouldReturnTraceNotProcessedError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.TraceNotProcessed);
    }

    [Fact]
    public async Task Handle_WhenTraceNotProcessed_ShouldReturnTraceNotProcessedError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateUploadedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.TraceNotProcessed);
    }

    #endregion

    #region Handle - Execution Already Running

    [Fact]
    public async Task Handle_WhenExecutionAlreadyRunning_ShouldReturnTraceAlreadyRunningError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        _executionRepository
            .HasRunningExecutionForTraceAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.TraceAlreadyRunning);
    }

    [Fact]
    public async Task Handle_WhenExecutionAlreadyRunning_ShouldNotPublishJob()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        _executionRepository
            .HasRunningExecutionForTraceAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _jobPublisher.DidNotReceive().PublishPipelineJobAsync(
            Arg.Any<PipelineJob>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new ExecutePipelineCommand(
            "invalid-user-id",
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

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
        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            "invalid-pipeline-id",
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.NotFound);
    }

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldReturnTraceNotProcessedError()
    {
        // Arrange
        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            "invalid-trace-id");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.TraceNotProcessed);
    }

    #endregion

    #region Handle - SaveChanges and Job Publishing

    [Fact]
    public async Task Handle_WhenPipelineNotFound_ShouldNotCallSaveChanges()
    {
        // Arrange
        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns((Pipeline?)null);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _jobPublisher.DidNotReceive().PublishPipelineJobAsync(
            Arg.Any<PipelineJob>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTraceNotProcessed_ShouldNotCallSaveChanges()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateUploadedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _jobPublisher.DidNotReceive().PublishPipelineJobAsync(
            Arg.Any<PipelineJob>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnExecutionId()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessedTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenTraceIsProcessing_ShouldReturnTraceNotProcessedError()
    {
        // Arrange
        var ownerId = new UserId(1);
        var pipeline = CreateActivePipeline(ownerId);
        var trace = CreateProcessingTrace();

        _pipelineRepository
            .GetByIdWithStepsAsync(Arg.Any<PipelineId>(), Arg.Any<CancellationToken>())
            .Returns(pipeline);

        _traceRepository
            .GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        var command = new ExecutePipelineCommand(
            ValidUserId,
            ValidStudyId,
            ValidPipelineId,
            ValidTraceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PipelineErrors.TraceNotProcessed);
    }

    #endregion
}
