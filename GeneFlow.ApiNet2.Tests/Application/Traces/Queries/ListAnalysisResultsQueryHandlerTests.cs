using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Queries.ListAnalysisResults;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for <see cref="ListAnalysisResultsQueryHandler"/>.
/// Verifies the union of cache- and datalake-known analysis types is returned.
/// </summary>
public class ListAnalysisResultsQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly IDatalakeStorageClient _datalakeClient = Substitute.For<IDatalakeStorageClient>();
    private readonly IAnalysisResultStore _resultStore = Substitute.For<IAnalysisResultStore>();
    private readonly ListAnalysisResultsQueryHandler _handler;

    private const string ValidUserId = "U00000001";

    public ListAnalysisResultsQueryHandlerTests()
    {
        _handler = new ListAnalysisResultsQueryHandler(_traceRepository, _datalakeClient, _resultStore);
    }

    [Fact]
    public async Task Handle_WhenOnlyCacheHasResults_ShouldReturnCachedTypes()
    {
        var trace = CreateProcessedTrace();
        var query = new ListAnalysisResultsQuery(ValidUserId, trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _resultStore.ListAsync(trace.Id.ToString(), Arg.Any<CancellationToken>())
            .Returns(new List<AnalysisResultMetadata>
            {
                new("trimming", DateTime.UtcNow),
                new("orf", DateTime.UtcNow)
            });
        _datalakeClient.ListAnalysisResultsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new[] { "trimming", "orf" });
    }

    [Fact]
    public async Task Handle_WhenCacheAndDatalakeOverlap_ShouldDeduplicateCaseInsensitively()
    {
        var trace = CreateProcessedTrace();
        var query = new ListAnalysisResultsQuery(ValidUserId, trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _resultStore.ListAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<AnalysisResultMetadata>
            {
                new("trimming", DateTime.UtcNow),
                new("orf", DateTime.UtcNow)
            });
        _datalakeClient.ListAnalysisResultsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new[] { "TRIMMING", "motif" });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(t => string.Equals(t, "trimming", StringComparison.OrdinalIgnoreCase));
        result.Value.Should().Contain(t => string.Equals(t, "orf", StringComparison.OrdinalIgnoreCase));
        result.Value.Should().Contain(t => string.Equals(t, "motif", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WhenBothEmpty_ShouldReturnEmptyList()
    {
        var trace = CreateProcessedTrace();
        var query = new ListAnalysisResultsQuery(ValidUserId, trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _resultStore.ListAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AnalysisResultMetadata>());
        _datalakeClient.ListAnalysisResultsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenTraceNotFound_ShouldReturnNotFound()
    {
        var query = new ListAnalysisResultsQuery(ValidUserId, TraceId.New().ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldReturnInvalidUserIdError()
    {
        var query = new ListAnalysisResultsQuery("invalid-user", TraceId.New().ToString());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
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
