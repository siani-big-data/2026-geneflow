using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetAnalysisResult;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for <see cref="GetAnalysisResultQueryHandler"/>.
/// Verifies the read-through behaviour: cache first, datalake fallback, cache write-back.
/// </summary>
public class GetAnalysisResultQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly IDatalakeStorageClient _datalakeClient = Substitute.For<IDatalakeStorageClient>();
    private readonly IAnalysisResultStore _resultStore = Substitute.For<IAnalysisResultStore>();
    private readonly GetAnalysisResultQueryHandler _handler;

    private const string ValidUserId = "U00000001";
    private const string AnalysisType = "trimming";
    private const string CachedJson = "{\"trimStart\":10,\"trimEnd\":480}";
    private const string DatalakeJson = "{\"trimStart\":15,\"trimEnd\":490}";

    public GetAnalysisResultQueryHandlerTests()
    {
        _handler = new GetAnalysisResultQueryHandler(_traceRepository, _datalakeClient, _resultStore);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedValueAndSkipDatalake()
    {
        var trace = CreateProcessedTrace();
        var query = new GetAnalysisResultQuery(ValidUserId, trace.Id.ToString(), AnalysisType);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _resultStore.GetAsync(trace.Id.ToString(), AnalysisType, Arg.Any<CancellationToken>())
            .Returns(CachedJson);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(CachedJson);

        await _datalakeClient.DidNotReceive().GetAnalysisResultJsonAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _resultStore.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndDatalakeHit_ShouldReturnDatalakeValueAndPopulateCache()
    {
        var trace = CreateProcessedTrace();
        var query = new GetAnalysisResultQuery(ValidUserId, trace.Id.ToString(), AnalysisType);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _resultStore.GetAsync(trace.Id.ToString(), AnalysisType, Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _datalakeClient.GetAnalysisResultJsonAsync(trace.Id.ToString(), AnalysisType, Arg.Any<CancellationToken>())
            .Returns(DatalakeJson);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(DatalakeJson);

        await _resultStore.Received(1).SaveAsync(
            trace.Id.ToString(), AnalysisType, DatalakeJson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndDatalakeMiss_ShouldReturnAnalysisTypeNotFound()
    {
        var trace = CreateProcessedTrace();
        var query = new GetAnalysisResultQuery(ValidUserId, trace.Id.ToString(), AnalysisType);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _resultStore.GetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _datalakeClient.GetAnalysisResultJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnalysisTypeNotFound");
    }

    [Fact]
    public async Task Handle_WhenTraceNotFound_ShouldReturnNotFoundAndNotTouchStores()
    {
        var query = new GetAnalysisResultQuery(ValidUserId, TraceId.New().ToString(), AnalysisType);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _resultStore.DidNotReceive().GetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _datalakeClient.DidNotReceive().GetAnalysisResultJsonAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnInvalidUserIdError()
    {
        var query = new GetAnalysisResultQuery("not-a-user-id", TraceId.New().ToString(), AnalysisType);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithEmptyAnalysisType_ShouldReturnAnalysisResultNotFound()
    {
        var trace = CreateProcessedTrace();
        var query = new GetAnalysisResultQuery(ValidUserId, trace.Id.ToString(), "   ");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnalysisResult");
    }

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
        var metrics = QualityMetrics.Create(35, 500, 90, 80, 500, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }
}
