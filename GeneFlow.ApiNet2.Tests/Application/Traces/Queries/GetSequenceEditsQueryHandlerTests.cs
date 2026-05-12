using GeneFlow.ApiNet2.Application.Traces.Queries.GetSequenceEdits;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for GetSequenceEditsQueryHandler.
/// </summary>
public class GetSequenceEditsQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly GetSequenceEditsQueryHandler _handler;

    public GetSequenceEditsQueryHandlerTests()
    {
        _handler = new GetSequenceEditsQueryHandler(_traceRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithActiveEdits_ShouldReturnActiveEditsOnly()
    {
        // Arrange
        var trace = CreateProcessedTraceWithEdits();
        var query = new GetSequenceEditsQuery(trace.Id.ToString(), IncludeInactive: false);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().OnlyContain(e => e.IsActive);
    }

    [Fact]
    public async Task Handle_WithIncludeInactiveTrue_ShouldReturnAllEdits()
    {
        // Arrange
        var trace = CreateProcessedTraceWithMixedEdits();
        var query = new GetSequenceEditsQuery(trace.Id.ToString(), IncludeInactive: true);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3); // All edits including inactive
    }

    [Fact]
    public async Task Handle_ShouldReturnEditsOrderedByPosition()
    {
        // Arrange
        var trace = CreateProcessedTraceWithEdits();
        var query = new GetSequenceEditsQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var positions = result.Value.Select(e => e.Position).ToList();
        positions.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_WithNoEdits_ShouldReturnEmptyList()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetSequenceEditsQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldFail()
    {
        // Arrange
        var query = new GetSequenceEditsQuery("invalid-trace-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentTrace_ShouldFail()
    {
        // Arrange
        var query = new GetSequenceEditsQuery(Guid.NewGuid().ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
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

    private static Trace CreateProcessedTraceWithEdits()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");
        trace.AddEdit(EditType.Change, 300, 'A', 'G', null, userId); // Added out of order to test sorting
        trace.AddEdit(EditType.Insert, 100, null, 'T', null, userId);
        trace.AddEdit(EditType.Delete, 200, 'C', null, null, userId);
        return trace;
    }

    private static Trace CreateProcessedTraceWithMixedEdits()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");
        trace.AddEdit(EditType.Change, 100, 'A', 'G', null, userId);
        var editResult = trace.AddEdit(EditType.Insert, 200, null, 'T', null, userId);
        trace.AddEdit(EditType.Delete, 300, 'C', null, null, userId);

        // Undo the middle edit to create an inactive edit
        trace.UndoEdit(editResult.Value.Id, userId);

        return trace;
    }

    #endregion
}
