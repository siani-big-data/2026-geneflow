using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetEditedSequence;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for GetEditedSequenceQueryHandler.
/// </summary>
public class GetEditedSequenceQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceAnalysisService _analysisService = Substitute.For<ITraceAnalysisService>();
    private readonly GetEditedSequenceQueryHandler _handler;

    private const string OriginalSequence = "ACGTACGTACGT";

    public GetEditedSequenceQueryHandlerTests()
    {
        _handler = new GetEditedSequenceQueryHandler(_traceRepository, _analysisService);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithNoEdits_ShouldReturnOriginalSequence()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetEditedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OriginalSequence.Should().Be(OriginalSequence);
        result.Value.EditedSequence.Should().Be(OriginalSequence);
        result.Value.EditCount.Should().Be(0);
        result.Value.AppliedEdits.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithChangeEdit_ShouldApplyChange()
    {
        // Arrange
        var trace = CreateProcessedTraceWithChangeEdit();
        var query = new GetEditedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EditedSequence.Should().Be("GCGTACGTACGT"); // First 'A' changed to 'G'
        result.Value.EditCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithInsertEdit_ShouldApplyInsertion()
    {
        // Arrange
        var trace = CreateProcessedTraceWithInsertEdit();
        var query = new GetEditedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EditedSequence.Should().Be("TACGTACGTACGT"); // 'T' inserted at position 0
        result.Value.EditCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithDeleteEdit_ShouldApplyDeletion()
    {
        // Arrange
        var trace = CreateProcessedTraceWithDeleteEdit();
        var query = new GetEditedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EditedSequence.Should().Be("CGTACGTACGT"); // First 'A' deleted
        result.Value.EditCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithMultipleEdits_ShouldApplyAllEdits()
    {
        // Arrange
        var trace = CreateProcessedTraceWithMultipleEdits();
        var query = new GetEditedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EditCount.Should().Be(2);
        result.Value.AppliedEdits.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectTraceId()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetEditedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TraceId.Should().Be(trace.Id.Value.ToString());
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldFail()
    {
        // Arrange
        var query = new GetEditedSequenceQuery("invalid-trace-id");

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
        var query = new GetEditedSequenceQuery(Guid.NewGuid().ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenSequenceRetrievalFails_ShouldReturnFailure()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetEditedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("Analysis.Failed", "Could not retrieve sequence")));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
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
        var metrics = QualityMetrics.Create(35, 12, 90, 80, 12, 45).Value; // 12 bases to match OriginalSequence length
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    private static Trace CreateProcessedTraceWithChangeEdit()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");
        trace.AddEdit(EditType.Change, 0, 'A', 'G', null, userId);
        return trace;
    }

    private static Trace CreateProcessedTraceWithInsertEdit()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");
        trace.AddEdit(EditType.Insert, 0, null, 'T', null, userId);
        return trace;
    }

    private static Trace CreateProcessedTraceWithDeleteEdit()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");
        trace.AddEdit(EditType.Delete, 0, 'A', null, null, userId);
        return trace;
    }

    private static Trace CreateProcessedTraceWithMultipleEdits()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");
        trace.AddEdit(EditType.Change, 0, 'A', 'G', null, userId);
        trace.AddEdit(EditType.Change, 4, 'A', 'T', null, userId);
        return trace;
    }

    #endregion
}
