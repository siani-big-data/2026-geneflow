using GeneFlow.ApiNet2.Application.Traces.Commands.CreateAnnotation;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.Events;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for CreateAnnotationCommandHandler.
/// </summary>
public class CreateAnnotationCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly CreateAnnotationCommandHandler _handler;

    private readonly string _validUserId = "U00000001";
    private readonly string _validTraceId = Guid.NewGuid().ToString();

    public CreateAnnotationCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new CreateAnnotationCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateAnnotation()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Label.Should().Be("Test Annotation");
        result.Value.Type.Should().Be("Region");
        result.Value.StartPosition.Should().Be(10);
        result.Value.EndPosition.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldPersistChanges()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPointAnnotationType_ShouldSetSameStartAndEnd()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand() with
        {
            TypeId = AnnotationType.Point.Id,
            StartPosition = 50,
            EndPosition = 100  // Should be normalized to 50 for Point type
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StartPosition.Should().Be(50);
        result.Value.EndPosition.Should().Be(50);
    }

    [Fact]
    public async Task Handle_WithSharedAnnotation_ShouldSetIsSharedTrue()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand() with { IsShared = true };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsShared.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithMetadata_ShouldStoreMetadata()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var metadata = System.Text.Json.JsonDocument.Parse("{\"key\": \"value\", \"nested\": {\"data\": 123}}");
        var command = CreateValidCommand() with { Metadata = metadata };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldRaiseAnnotationCreatedEvent()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.DomainEvents.Should().ContainSingle(e => e is AnnotationCreatedEvent);
        var domainEvent = trace.DomainEvents.OfType<AnnotationCreatedEvent>().Single();
        domainEvent.Label.Should().Be("Test Annotation");
        domainEvent.StartPosition.Should().Be(10);
        domainEvent.EndPosition.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDifferentStrands_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);

        // Test Plus strand
        var commandPlus = CreateValidCommand() with { StrandId = AnnotationStrand.Plus.Id };
        var resultPlus = await _handler.Handle(commandPlus, CancellationToken.None);
        resultPlus.IsSuccess.Should().BeTrue();
        resultPlus.Value.Strand.Should().Be("Plus");

        // Test Minus strand
        SetupTraceRepository(CreateProcessedTrace());
        var commandMinus = CreateValidCommand() with { StrandId = AnnotationStrand.Minus.Id };
        var resultMinus = await _handler.Handle(commandMinus, CancellationToken.None);
        resultMinus.IsSuccess.Should().BeTrue();
        resultMinus.Value.Strand.Should().Be("Minus");

        // Test None/Unknown strand
        SetupTraceRepository(CreateProcessedTrace());
        var commandNone = CreateValidCommand() with { StrandId = AnnotationStrand.None.Id };
        var resultNone = await _handler.Handle(commandNone, CancellationToken.None);
        resultNone.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validation Failures - Positions

    [Fact]
    public async Task Handle_WithNegativeStartPosition_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand() with { StartPosition = -1 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationOutOfBounds");
    }

    [Fact]
    public async Task Handle_WithEndPositionBeforeStart_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand() with { StartPosition = 100, EndPosition = 50 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidAnnotationRange");
    }

    [Fact]
    public async Task Handle_WithEndPositionBeyondSequence_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand() with { EndPosition = 2000 }; // Sequence is 1000 bases

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationOutOfBounds");
    }

    #endregion

    #region Validation Failures - Type and Strand

    [Fact]
    public async Task Handle_WithInvalidAnnotationType_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand() with { TypeId = 999 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidAnnotationType");
    }

    [Fact]
    public async Task Handle_WithInvalidAnnotationStrand_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand() with { StrandId = 999 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidAnnotationStrand");
    }

    #endregion

    #region Validation Failures - IDs and Permissions

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { UserId = "invalid-user-id" };

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
        var command = CreateValidCommand() with { TraceId = "invalid-trace-id" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenTraceNotFound_ShouldFail()
    {
        // Arrange
        _traceRepository.GetByIdWithAnnotationsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WhenTraceNotProcessed_ShouldFail()
    {
        // Arrange
        var trace = CreateUploadedTrace();
        SetupTraceRepository(trace);
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotEditInCurrentStatus");
    }

    #endregion

    #region Helper Methods

    private CreateAnnotationCommand CreateValidCommand()
    {
        return new CreateAnnotationCommand(
            UserId: _validUserId,
            TraceId: _validTraceId,
            TypeId: AnnotationType.Region.Id,
            Label: "Test Annotation",
            Description: "Test description",
            StartPosition: 10,
            EndPosition: 100,
            StrandId: AnnotationStrand.Plus.Id,
            Color: "#FF5733",
            IsShared: false);
    }

    private void SetupTraceRepository(Trace trace)
    {
        _traceRepository.GetByIdWithAnnotationsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
    }

    private Trace CreateUploadedTrace()
    {
        var traceId = TraceId.Parse(_validTraceId);
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse(_validUserId);
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/traces/sample.ab1", 1024, "abc123").Value;

        return Trace.Create(traceId, studyId, userId, traceName, null, traceFile, TraceFormat.AB1).Value;
    }

    private Trace CreateProcessedTrace()
    {
        var trace = CreateUploadedTrace();
        trace.StartProcessing();
        trace.TransitionToProcessing();
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    #endregion
}
