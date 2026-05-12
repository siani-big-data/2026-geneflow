using GeneFlow.ApiNet2.Application.Traces.Commands.CreateSequenceEdit;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for CreateSequenceEditCommandHandler.
/// </summary>
public class CreateSequenceEditCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly CreateSequenceEditCommandHandler _handler;

    public CreateSequenceEditCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new CreateSequenceEditCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidChangeEdit_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new CreateSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            EditType.Change.Id,
            100,
            'A',
            'G',
            "Correcting sequencing error");

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.EditType.Should().Be("Change");
        result.Value.Position.Should().Be(100);
        result.Value.OriginalBase.Should().Be('A');
        result.Value.NewBase.Should().Be('G');
        result.Value.Reason.Should().Be("Correcting sequencing error");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidInsertEdit_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new CreateSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            EditType.Insert.Id,
            200,
            null,
            'T',
            "Missing base");

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EditType.Should().Be("Insert");
        result.Value.Position.Should().Be(200);
        result.Value.OriginalBase.Should().BeNull();
        result.Value.NewBase.Should().Be('T');
    }

    [Fact]
    public async Task Handle_WithValidDeleteEdit_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new CreateSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            EditType.Delete.Id,
            300,
            'C',
            null,
            "Extra base");

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EditType.Should().Be("Delete");
        result.Value.Position.Should().Be(300);
        result.Value.OriginalBase.Should().Be('C');
        result.Value.NewBase.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new CreateSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            EditType.Change.Id,
            100,
            'A',
            'G',
            null);

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithLowercaseBase_ShouldNormalizeToUppercase()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new CreateSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            EditType.Change.Id,
            100,
            'a',
            'g',
            null);

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OriginalBase.Should().Be('A');
        result.Value.NewBase.Should().Be('G');
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var command = new CreateSequenceEditCommand(
            "invalid-user-id",
            Guid.NewGuid().ToString(),
            EditType.Change.Id,
            100,
            'A',
            'G',
            null);

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
        var command = new CreateSequenceEditCommand(
            "U00000001",
            "not-a-valid-guid",
            EditType.Change.Id,
            100,
            'A',
            'G',
            null);

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
        var command = new CreateSequenceEditCommand(
            "U00000001",
            Guid.NewGuid().ToString(),
            EditType.Change.Id,
            100,
            'A',
            'G',
            null);

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidEditType_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new CreateSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            999, // Invalid edit type ID
            100,
            'A',
            'G',
            null);

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidEditType");
    }

    [Fact]
    public async Task Handle_WithNegativePosition_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var command = new CreateSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            EditType.Change.Id,
            -1, // Negative position
            'A',
            'G',
            null);

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPosition");
    }

    [Fact]
    public async Task Handle_WithPositionExceedingSequenceLength_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace(); // Has 1000 bases
        var command = new CreateSequenceEditCommand(
            "U00000001",
            trace.Id.ToString(),
            EditType.Change.Id,
            1500, // Beyond sequence length
            'A',
            'G',
            null);

        _traceRepository.GetByIdWithEditsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPosition");
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

    #endregion
}
