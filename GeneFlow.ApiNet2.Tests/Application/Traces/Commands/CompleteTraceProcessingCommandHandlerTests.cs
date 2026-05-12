using GeneFlow.ApiNet2.Application.Traces.Commands.CompleteTraceProcessing;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for CompleteTraceProcessingCommandHandler.
/// </summary>
public class CompleteTraceProcessingCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly CompleteTraceProcessingCommandHandler _handler;

    public CompleteTraceProcessingCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new CompleteTraceProcessingCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidProcessingTrace_ShouldCompleteProcessing()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = CreateValidCommand(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Status.Should().Be(TraceStatus.Processed);
    }

    [Fact]
    public async Task Handle_ShouldUpdateQualityMetrics()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = new CompleteTraceProcessingCommand(
            trace.Id.ToString(),
            AverageQualityScore: 42.5m,
            TotalBases: 1500,
            QualityAboveQ20Percentage: 95.2m,
            QualityAboveQ30Percentage: 88.7m,
            TrimmedLength: 1400,
            GcContentPercentage: 52.3m,
            HasChromatogramData: true);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.QualityMetrics.Should().NotBeNull();
        trace.QualityMetrics!.AverageQualityScore.Should().Be(42.5m);
        trace.QualityMetrics.TotalBases.Should().Be(1500);
        trace.QualityMetrics.QualityAboveQ20Percentage.Should().Be(95.2m);
        trace.QualityMetrics.QualityAboveQ30Percentage.Should().Be(88.7m);
        trace.QualityMetrics.TrimmedLength.Should().Be(1400);
        trace.QualityMetrics.GcContentPercentage.Should().Be(52.3m);
    }

    [Fact]
    public async Task Handle_ShouldSetHasChromatogramData()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = CreateValidCommand(trace.Id.ToString()) with { HasChromatogramData = true };

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.HasChromatogramData.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSetProcessedAt()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = CreateValidCommand(trace.Id.ToString());
        var beforeProcessing = DateTime.UtcNow;

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ProcessedAt.Should().NotBeNull();
        trace.ProcessedAt.Should().BeOnOrAfter(beforeProcessing);
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = CreateValidCommand(trace.Id.ToString());

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
        var command = CreateValidCommand("invalid-trace-id");

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
        var command = CreateValidCommand(Guid.NewGuid().ToString());

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
        var command = CreateValidCommand(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public async Task Handle_WithFailedTrace_ShouldFail()
    {
        // Arrange
        var trace = CreateFailedTrace();
        var command = CreateValidCommand(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    #endregion

    #region Domain Events

    [Fact]
    public async Task Handle_ShouldRaiseProcessingCompletedEvent()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = CreateValidCommand(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TraceProcessedEvent");
    }

    #endregion

    #region Helper Methods

    private static CompleteTraceProcessingCommand CreateValidCommand(string traceId)
    {
        return new CompleteTraceProcessingCommand(
            TraceId: traceId,
            AverageQualityScore: 35.0m,
            TotalBases: 1000,
            QualityAboveQ20Percentage: 90.0m,
            QualityAboveQ30Percentage: 80.0m,
            TrimmedLength: 900,
            GcContentPercentage: 45.0m,
            HasChromatogramData: true);
    }

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

    private static Trace CreateFailedTrace(TraceId? id = null)
    {
        var trace = CreateProcessingTrace(id);
        trace.FailProcessing("Test failure reason");
        return trace;
    }

    #endregion
}
