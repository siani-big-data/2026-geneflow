using GeneFlow.ApiNet2.Application.Analysis;
using GeneFlow.ApiNet2.Application.Analysis.Commands.RequestAnalysis;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Tests.Application.Analysis.Commands;

/// <summary>
/// Unit tests for RequestAnalysisCommandHandler.
/// </summary>
public class RequestAnalysisCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly IJobPublisher _jobPublisher = Substitute.For<IJobPublisher>();
    private readonly ITraceAnalysisService _analysisService = Substitute.For<ITraceAnalysisService>();
    private readonly RequestAnalysisCommandHandler _handler;

    public RequestAnalysisCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _analysisService
            .GetAnalysisDataAsync(Arg.Any<Trace>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(("ACGT", new[] { 30, 30, 30, 30 })));

        _handler = new RequestAnalysisCommandHandler(_unitOfWork, _jobPublisher, _analysisService);
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidTraceId_ShouldPublishJob()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Trimming,
            new Dictionary<string, object> { { "threshold", 20 } });

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _jobPublisher.Received(1).PublishAnalysisJobAsync(
            Arg.Is<AnalysisJob>(job =>
                job.TraceId == trace.Id.ToString() &&
                job.AnalysisType == AnalysisTypes.Trimming &&
                job.Options != null &&
                job.Options.ContainsKey("threshold")),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AnalysisTypes.Quality)]
    [InlineData(AnalysisTypes.Trimming)]
    [InlineData(AnalysisTypes.Heterozygote)]
    [InlineData(AnalysisTypes.Motif)]
    [InlineData(AnalysisTypes.Translation)]
    [InlineData(AnalysisTypes.ORF)]
    [InlineData(AnalysisTypes.Restriction)]
    public async Task Handle_WithAllValidAnalysisTypes_ShouldReturnSuccess(string analysisType)
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            analysisType,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithCaseInsensitiveAnalysisType_ShouldReturnSuccess()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            "QUALITY", // uppercase
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNullOptions_ShouldPublishJobWithNullOptions()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            null!);

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _jobPublisher.Received(1).PublishAnalysisJobAsync(
            Arg.Is<AnalysisJob>(job => job.Options == null),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldReturnError()
    {
        // Arrange
        var command = new RequestAnalysisCommand(
            "not-a-valid-guid",
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.InvalidTraceId");
    }

    [Fact]
    public async Task Handle_WithEmptyTraceId_ShouldReturnError()
    {
        // Arrange
        var command = new RequestAnalysisCommand(
            string.Empty,
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.InvalidTraceId");
    }

    [Fact]
    public async Task Handle_WithNonExistentTrace_ShouldReturnNotFound()
    {
        // Arrange
        var traceId = TraceId.New();
        var command = new RequestAnalysisCommand(
            traceId.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.TraceNotFound");
    }

    [Fact]
    public async Task Handle_WithUnprocessedTrace_ShouldReturnError()
    {
        // Arrange
        var trace = CreateUploadedTrace(); // Not yet processed
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.TraceNotProcessed");
    }

    [Fact]
    public async Task Handle_WithProcessingTrace_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.TraceNotProcessed");
    }

    [Fact]
    public async Task Handle_WithValidatingTrace_ShouldReturnError()
    {
        // Arrange
        var trace = CreateValidatingTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.TraceNotProcessed");
    }

    [Fact]
    public async Task Handle_WithFailedTrace_ShouldReturnError()
    {
        // Arrange
        var trace = CreateFailedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.TraceNotProcessed");
    }

    [Fact]
    public async Task Handle_WithInvalidAnalysisType_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            "invalid-analysis-type",
            new Dictionary<string, object>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.InvalidAnalysisType");
    }

    [Fact]
    public async Task Handle_WithEmptyAnalysisType_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            string.Empty,
            new Dictionary<string, object>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.InvalidAnalysisType");
    }

    #endregion

    #region Repository Interactions

    [Fact]
    public async Task Handle_ShouldCallTraceRepository()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _traceRepository.Received(1).GetByIdAsync(
            Arg.Is<TraceId>(id => id == trace.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallJobPublisher()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Heterozygote,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _jobPublisher.Received(1).PublishAnalysisJobAsync(
            Arg.Any<AnalysisJob>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTraceNotFound_ShouldNotCallJobPublisher()
    {
        // Arrange
        var traceId = TraceId.New();
        var command = new RequestAnalysisCommand(
            traceId.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _jobPublisher.DidNotReceive().PublishAnalysisJobAsync(
            Arg.Any<AnalysisJob>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTraceNotProcessed_ShouldNotCallJobPublisher()
    {
        // Arrange
        var trace = CreateUploadedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _jobPublisher.DidNotReceive().PublishAnalysisJobAsync(
            Arg.Any<AnalysisJob>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInvalidAnalysisType_ShouldNotCallRepository()
    {
        // Arrange
        var traceId = TraceId.New();
        var command = new RequestAnalysisCommand(
            traceId.ToString(),
            "invalid-type",
            new Dictionary<string, object>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _traceRepository.DidNotReceive().GetByIdAsync(
            Arg.Any<TraceId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPublishJobWithSequenceAndQualityFromAnalysisService()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Translation,
            new Dictionary<string, object> { { "frame", 1 } });

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _jobPublisher.Received(1).PublishAnalysisJobAsync(
            Arg.Is<AnalysisJob>(job =>
                job.Sequence == "ACGT" &&
                job.Quality != null &&
                job.Quality.Length == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithArchivedTrace_ShouldReturnError()
    {
        // Arrange
        var trace = CreateArchivedTrace();
        var command = new RequestAnalysisCommand(
            trace.Id.ToString(),
            AnalysisTypes.Quality,
            new Dictionary<string, object>());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Analysis.TraceNotProcessed");
    }

    #endregion

    #region Helper Methods

    private static Trace CreateUploadedTrace()
    {
        var traceId = TraceId.New();
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse("U00000001");
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceDescription = TraceDescription.Create("Test trace").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/path", 1024, "checksum").Value;

        return Trace.Create(traceId, studyId, userId, traceName, traceDescription, traceFile, TraceFormat.AB1).Value;
    }

    private static Trace CreateValidatingTrace()
    {
        var trace = CreateUploadedTrace();
        trace.StartProcessing();
        return trace;
    }

    private static Trace CreateProcessingTrace()
    {
        var trace = CreateValidatingTrace();
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

    private static Trace CreateFailedTrace()
    {
        var trace = CreateProcessingTrace();
        trace.FailProcessing("Test failure");
        return trace;
    }

    private static Trace CreateArchivedTrace()
    {
        var trace = CreateProcessedTrace();
        trace.Archive(UserId.Parse("U00000001"));
        return trace;
    }

    #endregion
}
