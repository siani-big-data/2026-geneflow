using GeneFlow.ApiNet2.Application.Traces.Commands.UndoSequenceEdit;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for UndoSequenceEditCommandHandler.
/// </summary>
public class UndoSequenceEditCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly UndoSequenceEditCommandHandler _handler;

    public UndoSequenceEditCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new UndoSequenceEditCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidEdit_ShouldUndoSuccessfully()
    {
        // Arrange
        var trace = CreateProcessedTraceWithEdit();
        var editId = trace.Edits.First().Id;
        var command = new UndoSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            editId.ToString());

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveEditCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges()
    {
        // Arrange
        var trace = CreateProcessedTraceWithEdit();
        var editId = trace.Edits.First().Id;
        var command = new UndoSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            editId.ToString());

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleEdits_ShouldUndoOnlySpecifiedEdit()
    {
        // Arrange
        var trace = CreateProcessedTraceWithMultipleEdits();
        var firstEditId = trace.Edits.First().Id;
        var command = new UndoSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            firstEditId.ToString());

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveEditCount.Should().Be(2); // Started with 3, undid 1
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var command = new UndoSequenceEditCommand(
            "invalid-user-id",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithNonExistentTrace_ShouldFail()
    {
        // Arrange
        var command = new UndoSequenceEditCommand(
            "U00000001",
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentEdit_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTraceWithEdit();
        var command = new UndoSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            Guid.NewGuid().ToString()); // Non-existent edit ID

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EditNotFound");
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

    private static Trace CreateProcessingTrace()
    {
        var trace = CreateUploadedTrace();
        trace.StartProcessing();
        trace.TransitionToProcessing();
        return trace;
    }

    private static Trace CreateProcessedTrace()
    {
        var trace = CreateProcessingTrace();
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    private static Trace CreateProcessedTraceWithEdit()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");
        trace.AddEdit(EditType.Change, 100, 'A', 'G', "Test edit", userId);
        return trace;
    }

    private static Trace CreateProcessedTraceWithMultipleEdits()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");
        trace.AddEdit(EditType.Change, 100, 'A', 'G', null, userId);
        trace.AddEdit(EditType.Insert, 200, null, 'T', null, userId);
        trace.AddEdit(EditType.Delete, 300, 'C', null, null, userId);
        return trace;
    }

    #endregion
}
