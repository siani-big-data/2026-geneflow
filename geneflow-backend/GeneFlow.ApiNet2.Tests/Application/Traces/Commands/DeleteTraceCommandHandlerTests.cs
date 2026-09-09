using GeneFlow.ApiNet2.Application.Traces.Commands.DeleteTrace;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for DeleteTraceCommandHandler.
/// </summary>
public class DeleteTraceCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly DeleteTraceCommandHandler _handler;

    public DeleteTraceCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new DeleteTraceCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldDeleteTrace()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new DeleteTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new DeleteTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _traceRepository.Received(1).Update(trace);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUploadedTrace_ShouldDelete()
    {
        // Arrange
        var trace = CreateUploadedTrace();
        var command = new DeleteTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFailedTrace_ShouldDelete()
    {
        // Arrange
        var trace = CreateFailedTrace();
        var command = new DeleteTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithArchivedTrace_ShouldDelete()
    {
        // Arrange
        var trace = CreateArchivedTrace();
        var command = new DeleteTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var command = new DeleteTraceCommand("invalid-user-id", Guid.NewGuid().ToString());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldFail()
    {
        // Arrange
        var command = new DeleteTraceCommand("U00000001", "not-a-guid");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentTrace_ShouldFail()
    {
        // Arrange
        var command = new DeleteTraceCommand("U00000001", Guid.NewGuid().ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithProcessingTrace_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var command = new DeleteTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotDeleteWhileProcessing");
    }

    [Fact]
    public async Task Handle_WithValidatingTrace_ShouldFail()
    {
        // Arrange
        var trace = CreateValidatingTrace();
        var command = new DeleteTraceCommand("U00000001", trace.Id.ToString());

        _traceRepository.GetByIdAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotDeleteWhileProcessing");
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
