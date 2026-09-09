using GeneFlow.ApiNet2.Application.Traces.Commands.AutoTrimTrace;
using GeneFlow.ApiNet2.Application.Traces.Interfaces;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for AutoTrimTraceCommandHandler.
/// Tests auto-trimming functionality using quality-based algorithms.
/// </summary>
public class AutoTrimTraceCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly ITraceAnalysisService _analysisService = Substitute.For<ITraceAnalysisService>();
    private readonly AutoTrimTraceCommandHandler _handler;

    public AutoTrimTraceCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new AutoTrimTraceCommandHandler(_unitOfWork, _analysisService);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldApplyAutoTrim()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString());
        var qualityScores = CreateQualityScoresWithLowEnds();

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        trace.Trims.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithLowQuality5PrimeEnd_ShouldCreate5PrimeTrim()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString(), QualityThreshold: 20, WindowSize: 5);
        // Quality scores: first 50 bases are low quality (10), rest are high quality (30)
        var qualityScores = CreateQualityScoresWithLow5Prime(50, 1000);

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(t => t.TrimEnd == "FivePrime");
    }

    [Fact]
    public async Task Handle_WithLowQuality3PrimeEnd_ShouldCreate3PrimeTrim()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString(), QualityThreshold: 20, WindowSize: 5);
        // Quality scores: last 50 bases are low quality (10), rest are high quality (30)
        var qualityScores = CreateQualityScoresWithLow3Prime(50, 1000);

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(t => t.TrimEnd == "ThreePrime");
    }

    [Fact]
    public async Task Handle_ShouldPersistTrims()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString());
        var qualityScores = CreateQualityScoresWithLowEnds();

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _traceRepository.Received(1).Update(trace);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCalculateQualityThresholds()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var customThreshold = 25;
        var customWindowSize = 8;
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString(),
            QualityThreshold: customThreshold, WindowSize: customWindowSize);
        var qualityScores = CreateQualityScoresWithLowEnds();

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(dto =>
        {
            dto.Algorithm.Should().Contain($"Q{customThreshold}");
            dto.Algorithm.Should().Contain($"W{customWindowSize}");
        });
    }

    [Fact]
    public async Task Handle_ShouldApplyTrimsToSequence()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString(), QualityThreshold: 20, WindowSize: 5);
        var qualityScores = CreateQualityScoresWithLowEnds();

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Trims.Should().NotBeEmpty();
        // Verify that trims were created for the low quality regions
        var fivePrimeTrim = result.Value.FirstOrDefault(t => t.TrimEnd == "FivePrime");
        var threePrimeTrim = result.Value.FirstOrDefault(t => t.TrimEnd == "ThreePrime");
        // At least one trim should be created based on quality scores
        (fivePrimeTrim != null || threePrimeTrim != null).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRaiseTrimmedEvent()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString());
        var qualityScores = CreateQualityScoresWithLowEnds();

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Domain events are raised when AddTrim is called on the trace
        // The trace should have domain events queued for each trim added
        trace.DomainEvents.Should().Contain(e => e.GetType().Name.Contains("TrimApplied"));
    }

    [Fact]
    public async Task Handle_ShouldReturnTrimDtos()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString());
        var qualityScores = CreateQualityScoresWithLowEnds();

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(dto =>
        {
            dto.Id.Should().NotBeNullOrEmpty();
            dto.TrimType.Should().Be("AutoMott");
            dto.Algorithm.Should().Contain("Mott");
            dto.IsActive.Should().BeTrue();
        });
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var command = new AutoTrimTraceCommand("invalid-user-id", Guid.NewGuid().ToString());

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
        var command = new AutoTrimTraceCommand("U00000001", Guid.NewGuid().ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_TraceNotProcessed_ShouldReturnError()
    {
        // Arrange
        var trace = CreateUploadedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotEditInCurrentStatus");
    }

    [Fact]
    public async Task Handle_QualityScoresNotAvailable_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<int[]>(TraceErrors.AnalysisResultNotFound));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnalysisResultNotFound");
    }

    [Fact]
    public async Task Handle_TrimmedSequenceTooShort_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        // Request minimum length of 900 bases
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString(), MinimumLength: 900);
        // Quality scores that would trim too much (leaving only 100 good bases)
        var qualityScores = CreateQualityScoresWithMostlyLowQuality();

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimmedTooShort");
    }

    [Fact]
    public async Task Handle_EmptyQualityScores_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new AutoTrimTraceCommand("U00000001", trace.Id.ToString());
        var qualityScores = Array.Empty<int>();

        _traceRepository.GetByIdWithTrimsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
        _analysisService.GetQualityScoresAsync(trace, Arg.Any<CancellationToken>())
            .Returns(Result.Success(qualityScores));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTotalBases");
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
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    /// <summary>
    /// Creates quality scores with low quality at both 5' and 3' ends.
    /// </summary>
    private static int[] CreateQualityScoresWithLowEnds()
    {
        var scores = new int[1000];
        // First 30 bases: low quality (5-15)
        for (int i = 0; i < 30; i++)
            scores[i] = 10;
        // Middle: high quality (25-40)
        for (int i = 30; i < 970; i++)
            scores[i] = 35;
        // Last 30 bases: low quality (5-15)
        for (int i = 970; i < 1000; i++)
            scores[i] = 10;
        return scores;
    }

    /// <summary>
    /// Creates quality scores with low quality only at 5' end.
    /// </summary>
    private static int[] CreateQualityScoresWithLow5Prime(int lowQualityBases, int totalBases)
    {
        var scores = new int[totalBases];
        for (int i = 0; i < totalBases; i++)
            scores[i] = i < lowQualityBases ? 10 : 30;
        return scores;
    }

    /// <summary>
    /// Creates quality scores with low quality only at 3' end.
    /// </summary>
    private static int[] CreateQualityScoresWithLow3Prime(int lowQualityBases, int totalBases)
    {
        var scores = new int[totalBases];
        for (int i = 0; i < totalBases; i++)
            scores[i] = i >= (totalBases - lowQualityBases) ? 10 : 30;
        return scores;
    }

    /// <summary>
    /// Creates quality scores where most of the sequence is low quality.
    /// </summary>
    private static int[] CreateQualityScoresWithMostlyLowQuality()
    {
        var scores = new int[1000];
        // First 450 bases: low quality
        for (int i = 0; i < 450; i++)
            scores[i] = 10;
        // Middle 100 bases: high quality
        for (int i = 450; i < 550; i++)
            scores[i] = 35;
        // Last 450 bases: low quality
        for (int i = 550; i < 1000; i++)
            scores[i] = 10;
        return scores;
    }

    #endregion
}
