using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetReverseComplement;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for GetReverseComplementQueryHandler.
/// </summary>
public class GetReverseComplementQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceAnalysisService _analysisService = Substitute.For<ITraceAnalysisService>();
    private readonly GetReverseComplementQueryHandler _handler;

    public GetReverseComplementQueryHandlerTests()
    {
        _handler = new GetReverseComplementQueryHandler(_traceRepository, _analysisService);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidTrace_ShouldReturnReverseComplement()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetReverseComplementQuery(trace.Id.ToString(), UseTrimmedSequence: false);
        var originalSequence = "ATCG";

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(originalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TraceId.Should().Be(trace.Id.ToString());
        result.Value.OriginalSequence.Should().Be(originalSequence);
        result.Value.ReverseComplementSequence.Should().Be("CGAT"); // Complement of ATCG reversed
    }

    [Fact]
    public async Task Handle_ShouldComputeCorrectReverseComplement()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetReverseComplementQuery(trace.Id.ToString(), UseTrimmedSequence: false);
        // A -> T, T -> A, C -> G, G -> C, then reverse
        var originalSequence = "AATTCCGG";

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(originalSequence));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Original: AATTCCGG
        // Complement: TTAAGGCC
        // Reverse: CCGGAATT
        result.Value.ReverseComplementSequence.Should().Be("CCGGAATT");
        result.Value.Length.Should().Be(8);
    }

    [Fact]
    public async Task Handle_WithUseTrimmedSequenceTrue_ShouldUseTrimsRepository()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetReverseComplementQuery(trace.Id.ToString(), UseTrimmedSequence: true);

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetSequenceAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success("ATCG"));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _traceRepository.Received(1).GetByIdWithTrimsAsync(
            Arg.Any<TraceId>(),
            Arg.Any<CancellationToken>());
        await _traceRepository.DidNotReceive().GetByIdAsync(
            Arg.Any<TraceId>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldFail()
    {
        // Arrange
        var query = new GetReverseComplementQuery("invalid-trace-id");

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
        var query = new GetReverseComplementQuery(Guid.NewGuid().ToString(), UseTrimmedSequence: false);

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

    private static Trace CreateProcessedTrace(TraceId? id = null)
    {
        var trace = CreateProcessingTrace(id);
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    private static Trace CreateProcessingTrace(TraceId? id = null)
    {
        var trace = CreateUploadedTrace(id);
        trace.StartProcessing();
        trace.TransitionToProcessing();
        return trace;
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

    #endregion
}
