using GeneFlow.ApiNet2.Application.Traces.Commands.UndoTrimTrace;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for UndoTrimTraceCommandHandler.
/// Tests undoing specific trims or all trims from a trace.
/// </summary>
public class UndoTrimTraceCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly UndoTrimTraceCommandHandler _handler;

    public UndoTrimTraceCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new UndoTrimTraceCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_UndoSpecificTrim_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var trimId = trace.Trims.First().Id;
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), trimId.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveTrimCount.Should().Be(1); // One trim undone, one remains
    }

    [Fact]
    public async Task Handle_UndoAllTrims_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), UndoAll: true);

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveTrimCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var trimId = trace.Trims.First().Id;
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), trimId.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _traceRepository.Received(1).Update(trace);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRestoreSequence()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var initialActiveTrimCount = trace.ActiveTrimCount;
        var trimId = trace.Trims.First().Id;
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), trimId.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // One trim should now be inactive (undone)
        trace.ActiveTrimCount.Should().BeLessThan(initialActiveTrimCount);
        // The undone trim should be marked as inactive
        var undoneTrim = trace.Trims.First(t => t.Id == trimId);
        undoneTrim.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldRaiseTrimUndoneEvent()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var trimId = trace.Trims.First().Id;
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), trimId.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Domain events are raised when UndoTrim is called on the trace
        trace.DomainEvents.Should().Contain(e => e.GetType().Name.Contains("TrimUndone"));
    }

    [Fact]
    public async Task Handle_UndoAll_ShouldRestoreAllTrims()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var initialTrimCount = trace.Trims.Count;
        initialTrimCount.Should().BeGreaterThan(0);
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), UndoAll: true);

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveTrimCount.Should().Be(0);
        // All trims should now be inactive
        trace.Trims.Should().OnlyContain(t => !t.IsActive);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var command = new UndoTrimTraceCommand("invalid-user-id", Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

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
        var command = new UndoTrimTraceCommand("U00000001", Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_TrimNotFound_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var nonExistentTrimId = Guid.NewGuid().ToString();
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), nonExistentTrimId);

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimNotFound");
    }

    [Fact]
    public async Task Handle_NoActiveTrims_UndoAll_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace(); // No trims
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), UndoAll: true);

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoActiveTrims");
    }

    [Fact]
    public async Task Handle_NeitherTrimIdNorUndoAll_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        // Neither TrimId nor UndoAll specified
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimNotFound");
    }

    [Fact]
    public async Task Handle_InvalidTrimIdFormat_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var command = new UndoTrimTraceCommand("U00000001", trace.Id.ToString(), "not-a-valid-guid");

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimNotFound");
    }

    #endregion

    #region Helper Methods

    private static Trace CreateProcessedTrace()
    {
        var traceId = TraceId.New();
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse("U00000001");
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceDescription = TraceDescription.Create("Test").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/path", 1024, "checksum").Value;

        var trace = Trace.Create(traceId, studyId, userId, traceName, traceDescription, traceFile, TraceFormat.AB1).Value;
        trace.StartProcessing();
        trace.TransitionToProcessing();
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    private static Trace CreateProcessedTraceWithTrims()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");

        // Add two trims
        trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", userId, "5' trim");
        trace.AddTrim(TrimType.Manual, 950, 1000, TrimEnd.ThreePrime, "Manual", userId, "3' trim");

        return trace;
    }

    #endregion
}
