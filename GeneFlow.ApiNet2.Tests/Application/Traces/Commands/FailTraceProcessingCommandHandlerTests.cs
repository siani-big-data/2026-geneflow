using GeneFlow.ApiNet2.Application.Traces.Commands.FailTraceProcessing;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for FailTraceProcessingCommandHandler.
/// </summary>
public class FailTraceProcessingCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly FailTraceProcessingCommandHandler _handler;

    public FailTraceProcessingCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new FailTraceProcessingCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidProcessingTrace_ShouldMarkAsFailed()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = new FailTraceProcessingCommand(trace.Id.ToString(), "Processing failed due to invalid file format");

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Status.Should().Be(TraceStatus.Failed);
    }

    [Fact]
    public async Task Handle_ShouldSaveFailureReason()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var failureReason = "Invalid chromatogram data: corrupted peak values";
        var command = new FailTraceProcessingCommand(trace.Id.ToString(), failureReason);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.FailureReason.Should().Be(failureReason);
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = new FailTraceProcessingCommand(trace.Id.ToString(), "Test failure");

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _traceRepository.Received(1).Update(trace);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldFail()
    {
        // Arrange
        var command = new FailTraceProcessingCommand("invalid-trace-id", "Test failure");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentTrace_ShouldFail()
    {
        // Arrange
        var command = new FailTraceProcessingCommand(Guid.NewGuid().ToString(), "Test failure");

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithUploadedTrace_ShouldFail()
    {
        // Arrange
        var trace = CreateUploadedTrace();
        var command = new FailTraceProcessingCommand(trace.Id.ToString(), "Test failure");

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public async Task Handle_WithProcessedTrace_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new FailTraceProcessingCommand(trace.Id.ToString(), "Test failure");

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public async Task Handle_WithEmptyReason_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = new FailTraceProcessingCommand(trace.Id.ToString(), string.Empty);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FailureReasonRequired");
    }

    [Fact]
    public async Task Handle_WithWhitespaceReason_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = new FailTraceProcessingCommand(trace.Id.ToString(), "   ");

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FailureReasonRequired");
    }

    #endregion

    #region Domain Events

    [Fact]
    public async Task Handle_ShouldRaiseProcessingFailedEvent()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var failureReason = "Processing failed due to invalid data";
        var command = new FailTraceProcessingCommand(trace.Id.ToString(), failureReason);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TraceProcessingFailedEvent");
    }

    #endregion

    #region Helper Methods

    private static Trace CreateUploadedTrace(TraceId? id = null)
    {
        var traceId = id ?? TraceId.New();
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse("U00000001");
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceDescription = TraceDescription.Create("Test description").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/path", 1024, "checksum").Value;

        return Trace.Create(traceId, studyId, userId, traceName, traceDescription, traceFile, TraceFormat.AB1).Value;
    }

    private static Trace CreateProcessingTrace(TraceId? id = null)
    {
        var trace = CreateUploadedTrace(id);
        trace.StartProcessing();
        trace.TransitionToProcessing();
        return trace;
    }

    private static Trace CreateProcessedTrace(TraceId? id = null)
    {
        var trace = CreateProcessingTrace(id);
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    #endregion
}
