using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTrimmedSequence;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for GetTrimmedSequenceQueryHandler.
/// Tests retrieval of trimmed sequences with all active trim operations applied.
/// </summary>
public class GetTrimmedSequenceQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceAnalysisService _analysisService = Substitute.For<ITraceAnalysisService>();
    private readonly GetTrimmedSequenceQueryHandler _handler;

    private const string OriginalSequence = "ACGTACGTACGTACGTACGTACGTACGTACGTACGTACGTACGTACGTACGT"; // 52 bases

    public GetTrimmedSequenceQueryHandlerTests()
    {
        _handler = new GetTrimmedSequenceQueryHandler(_traceRepository, _analysisService);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidTraceId_ShouldReturnTrimmedSequence()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var query = new GetTrimmedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TraceId.Should().Be(trace.Id.Value.ToString());
        result.Value.OriginalSequence.Should().Be(OriginalSequence);
        result.Value.TrimmedSequence.Should().NotBeNullOrEmpty();
        result.Value.TrimmedLength.Should().BeLessThan(result.Value.OriginalLength);
        result.Value.TotalBasesTrimmed.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithMultipleTrims_ShouldApplyAllTrims()
    {
        // Arrange
        var trace = CreateProcessedTraceWithMultipleTrims();
        var query = new GetTrimmedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AppliedTrims.Should().HaveCount(2);
        result.Value.TrimmedLength.Should().BeLessThan(result.Value.OriginalLength);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectTrimStatistics()
    {
        // Arrange
        var trace = CreateProcessedTraceWith5PrimeTrim();
        var query = new GetTrimmedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OriginalLength.Should().Be(OriginalSequence.Length);
        result.Value.TotalBasesTrimmed.Should().Be(result.Value.OriginalLength - result.Value.TrimmedLength);
        result.Value.AppliedTrims.Should().HaveCount(1);
        result.Value.AppliedTrims.First().TrimEnd.Should().Be("FivePrime");
    }

    [Fact]
    public async Task Handle_With5PrimeTrim_ShouldTrimFromStart()
    {
        // Arrange
        var trace = CreateProcessedTraceWith5PrimeTrim();
        var query = new GetTrimmedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // 5' trim removes first 10 bases, so trimmed sequence should start after position 10
        result.Value.TrimmedSequence.Should().Be(OriginalSequence.Substring(10));
    }

    [Fact]
    public async Task Handle_With3PrimeTrim_ShouldTrimFromEnd()
    {
        // Arrange
        var trace = CreateProcessedTraceWith3PrimeTrim();
        var query = new GetTrimmedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(OriginalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // 3' trim removes last 10 bases (positions 42-52)
        result.Value.TrimmedSequence.Should().Be(OriginalSequence.Substring(0, 42));
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldFail()
    {
        // Arrange
        var query = new GetTrimmedSequenceQuery("invalid-trace-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentTrace_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetTrimmedSequenceQuery(Guid.NewGuid().ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNoTrims_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace(); // No trims
        var query = new GetTrimmedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoActiveTrims");
    }

    [Fact]
    public async Task Handle_WhenSequenceRetrievalFails_ShouldReturnFailure()
    {
        // Arrange
        var trace = CreateProcessedTraceWithTrims();
        var query = new GetTrimmedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.Failure("Analysis.Failed", "Could not retrieve sequence")));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAllTrimsUndone_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTraceWithUndoneTrims();
        var query = new GetTrimmedSequenceQuery(trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoActiveTrims");
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
        var metrics = QualityMetrics.Create(35, 52, 90, 80, 52, 45).Value; // 52 bases to match OriginalSequence length
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    private static Trace CreateProcessedTraceWithTrims()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");

        // Add a 5' trim (remove first 10 bases)
        trace.AddTrim(TrimType.Manual, 0, 10, TrimEnd.FivePrime, "Manual", userId, "5' trim");

        return trace;
    }

    private static Trace CreateProcessedTraceWithMultipleTrims()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");

        // Add 5' trim (first 10 bases)
        trace.AddTrim(TrimType.Manual, 0, 10, TrimEnd.FivePrime, "Manual", userId, "5' trim");
        // Add 3' trim (last 10 bases: positions 42-52)
        trace.AddTrim(TrimType.Manual, 42, 52, TrimEnd.ThreePrime, "Manual", userId, "3' trim");

        return trace;
    }

    private static Trace CreateProcessedTraceWith5PrimeTrim()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");

        // Add a 5' trim (remove first 10 bases)
        trace.AddTrim(TrimType.Manual, 0, 10, TrimEnd.FivePrime, "Manual", userId, "5' trim");

        return trace;
    }

    private static Trace CreateProcessedTraceWith3PrimeTrim()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");

        // Add a 3' trim (remove last 10 bases: positions 42-52)
        trace.AddTrim(TrimType.Manual, 42, 52, TrimEnd.ThreePrime, "Manual", userId, "3' trim");

        return trace;
    }

    private static Trace CreateProcessedTraceWithUndoneTrims()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse("U00000001");

        // Add trim and then undo it
        trace.AddTrim(TrimType.Manual, 0, 10, TrimEnd.FivePrime, "Manual", userId, "5' trim");
        trace.UndoAllTrims(userId);

        return trace;
    }

    #endregion
}
