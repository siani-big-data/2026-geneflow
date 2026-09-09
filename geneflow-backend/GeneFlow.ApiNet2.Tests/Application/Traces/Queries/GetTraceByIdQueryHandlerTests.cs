using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceById;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for GetTraceByIdQueryHandler.
/// </summary>
public class GetTraceByIdQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly GetTraceByIdQueryHandler _handler;

    public GetTraceByIdQueryHandlerTests()
    {
        _handler = new GetTraceByIdQueryHandler(_traceRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnTrace()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetTraceByIdQuery("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdWithAllAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(trace.Id.ToString());
        result.Value.Name.Should().Be(trace.Name.Value);
        result.Value.Status.Should().Be("Processed");
    }

    [Fact]
    public async Task Handle_ShouldReturnFullTraceDetails()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var query = new GetTraceByIdQuery("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdWithAllAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("sample.ab1");
        result.Value.Format.Should().Be("AB1");
        result.Value.HasChromatogramData.Should().BeTrue();
        result.Value.QualityMetrics.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithUploadedTrace_ShouldReturnNullMetrics()
    {
        // Arrange
        var trace = CreateUploadedTrace();
        var query = new GetTraceByIdQuery("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdWithAllAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Uploaded");
        result.Value.QualityMetrics.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithFailedTrace_ShouldReturnFailureReason()
    {
        // Arrange
        var trace = CreateFailedTrace();
        var query = new GetTraceByIdQuery("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdWithAllAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Failed");
        result.Value.FailureReason.Should().Be("Test failure reason");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var query = new GetTraceByIdQuery("invalid-user-id", Guid.NewGuid().ToString());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldFail()
    {
        // Arrange
        var query = new GetTraceByIdQuery("U00000001", "not-a-guid");

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
        var query = new GetTraceByIdQuery("U00000001", Guid.NewGuid().ToString());

        _traceRepository.GetByIdWithAllAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Repository Interaction

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var traceId = TraceId.New();
        var trace = CreateProcessedTrace(traceId);
        var query = new GetTraceByIdQuery("U00000001", traceId.ToString());

        _traceRepository.GetByIdWithAllAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _traceRepository.Received(1).GetByIdWithAllAsync(
            Arg.Is<TraceId>(id => id.Value == traceId.Value),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

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

    private static Trace CreateProcessedTrace(TraceId? id = null)
    {
        var trace = CreateProcessingTrace(id);
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
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
