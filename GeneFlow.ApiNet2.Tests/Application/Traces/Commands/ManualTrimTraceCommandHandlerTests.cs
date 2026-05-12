using GeneFlow.ApiNet2.Application.Traces.Commands.ManualTrimTrace;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for ManualTrimTraceCommandHandler.
/// Tests manual trimming functionality with user-specified positions.
/// </summary>
public class ManualTrimTraceCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly ManualTrimTraceCommandHandler _handler;

    public ManualTrimTraceCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new ManualTrimTraceCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldApplyManualTrim()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 0,
            EndPosition: 50,
            TrimEnd: "FivePrime",
            Reason: "Low quality region");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.StartPosition.Should().Be(0);
        result.Value.EndPosition.Should().Be(50);
        result.Value.TrimEnd.Should().Be("FivePrime");
    }

    [Fact]
    public async Task Handle_With3PrimeTrim_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 950,
            EndPosition: 1000,
            TrimEnd: "ThreePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrimEnd.Should().Be("ThreePrime");
        result.Value.Length.Should().Be(50);
    }

    [Fact]
    public async Task Handle_WithReason_ShouldSetReason()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var reason = "Removing primer region";
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 0,
            EndPosition: 25,
            TrimEnd: "FivePrime",
            Reason: reason);

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reason.Should().Be(reason);
    }

    [Fact]
    public async Task Handle_ShouldPersistTrim()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 0,
            EndPosition: 50,
            TrimEnd: "FivePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _traceRepository.Received(1).Update(trace);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldApplyTrim()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 0,
            EndPosition: 100,
            TrimEnd: "FivePrime",
            Reason: "Remove primer region");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Trims.Should().NotBeEmpty();
        trace.Trims.Should().Contain(t => t.StartPosition == 0 && t.EndPosition == 100);
        trace.ActiveTrimCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldRaiseTrimmedEvent()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 0,
            EndPosition: 50,
            TrimEnd: "FivePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Domain events are raised when AddTrim is called on the trace
        trace.DomainEvents.Should().Contain(e => e.GetType().Name.Contains("TrimApplied"));
    }

    [Fact]
    public async Task Handle_ShouldReturnTrimDto()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 100,
            EndPosition: 200,
            TrimEnd: "FivePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().NotBeNullOrEmpty();
        dto.TrimType.Should().Be("Manual");
        dto.Algorithm.Should().Be("Manual");
        dto.AppliedBy.Should().Be("U00000001");
        dto.IsActive.Should().BeTrue();
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var command = new ManualTrimTraceCommand(
            "invalid-user-id",
            Guid.NewGuid().ToString(),
            StartPosition: 0,
            EndPosition: 50,
            TrimEnd: "FivePrime");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_TraceNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var command = new ManualTrimTraceCommand(
            "U00000001",
            Guid.NewGuid().ToString(),
            StartPosition: 0,
            EndPosition: 50,
            TrimEnd: "FivePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_InvalidTrimEnd_ShouldReturnError()
    {
        // Arrange
        var command = new ManualTrimTraceCommand(
            "U00000001",
            Guid.NewGuid().ToString(),
            StartPosition: 0,
            EndPosition: 50,
            TrimEnd: "InvalidEnd");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimEnd");
    }

    [Fact]
    public async Task Handle_InvalidPositions_EndBeforeStart_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 100,
            EndPosition: 50, // End before start
            TrimEnd: "FivePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPositions");
    }

    [Fact]
    public async Task Handle_PositionsOutOfBounds_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace(); // Has 1000 bases
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 950,
            EndPosition: 1100, // Exceeds sequence length
            TrimEnd: "ThreePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimExceedsSequenceLength");
    }

    [Fact]
    public async Task Handle_NegativeStartPosition_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: -10,
            EndPosition: 50,
            TrimEnd: "FivePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPosition");
    }

    [Fact]
    public async Task Handle_TraceNotProcessed_ShouldReturnError()
    {
        // Arrange
        var trace = CreateUploadedTrace();
        var command = new ManualTrimTraceCommand(
            "U00000001",
            trace.Id.ToString(),
            StartPosition: 0,
            EndPosition: 50,
            TrimEnd: "FivePrime");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotEditInCurrentStatus");
    }

    #endregion

    #region Helper Methods

    private static Trace CreateUploadedTrace()
    {
        var traceId = TraceId.New();
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse("U00000001");
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceDescription = TraceDescription.Create("Test").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/path", 1024, "checksum").Value;

        return Trace.Create(traceId, studyId, userId, traceName, traceDescription, traceFile, TraceFormat.AB1).Value;
    }

    private static Trace CreateProcessedTrace()
    {
        var trace = CreateUploadedTrace();
        trace.StartProcessing();
        trace.TransitionToProcessing();
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    #endregion
}
