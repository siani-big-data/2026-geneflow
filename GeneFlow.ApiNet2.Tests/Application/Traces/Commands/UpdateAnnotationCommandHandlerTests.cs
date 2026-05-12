using GeneFlow.ApiNet2.Application.Traces.Commands.UpdateAnnotation;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.Events;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Commands;

/// <summary>
/// Unit tests for UpdateAnnotationCommandHandler.
/// </summary>
public class UpdateAnnotationCommandHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly ITraceUnitOfWork _unitOfWork = Substitute.For<ITraceUnitOfWork>();
    private readonly UpdateAnnotationCommandHandler _handler;

    private readonly string _validUserId = "U00000001";
    private readonly string _validTraceId = Guid.NewGuid().ToString();

    public UpdateAnnotationCommandHandlerTests()
    {
        _unitOfWork.Traces.Returns(_traceRepository);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        _handler = new UpdateAnnotationCommandHandler(_unitOfWork);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateAnnotation()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Label.Should().Be("Updated Label");
        result.Value.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldPersistChanges()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNewColor_ShouldUpdateColor()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { Color = "#00FF00" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Color.Should().Be("#00FF00");
    }

    [Fact]
    public async Task Handle_ChangingStrand_ShouldSucceed()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { StrandId = AnnotationStrand.Minus.Id };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Strand.Should().Be("Minus");
    }

    [Fact]
    public async Task Handle_ChangingPositions_ShouldSucceed()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { StartPosition = 50, EndPosition = 200 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StartPosition.Should().Be(50);
        result.Value.EndPosition.Should().Be(200);
    }

    [Fact]
    public async Task Handle_ChangingIsShared_ShouldSucceed()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { IsShared = true };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsShared.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRaiseAnnotationUpdatedEvent()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Filter to get only AnnotationUpdatedEvent (ignoring the AnnotationCreatedEvent from setup)
        trace.DomainEvents.Should().Contain(e => e is AnnotationUpdatedEvent);
        var domainEvent = trace.DomainEvents.OfType<AnnotationUpdatedEvent>().Single();
        domainEvent.AnnotationId.Should().Be(Guid.Parse(annotationId));
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPosition_ShouldReturnError()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { StartPosition = -1 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationOutOfBounds");
    }

    [Fact]
    public async Task Handle_WithEndBeforeStart_ShouldReturnError()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { StartPosition = 100, EndPosition = 50 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidAnnotationRange");
    }

    [Fact]
    public async Task Handle_WithPositionExceedingSequence_ShouldReturnError()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { EndPosition = 2000 }; // Sequence is 1000 bases

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationOutOfBounds");
    }

    [Fact]
    public async Task Handle_WithMalformedAnnotationId_ShouldReturnError()
    {
        // Arrange
        var (trace, _) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand("not-a-valid-guid");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationNotFound");
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldFail()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { UserId = "invalid-user-id" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidUserId");
    }

    [Fact]
    public async Task Handle_WithInvalidAnnotationId_ShouldFail()
    {
        // Arrange
        var (trace, _) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(Guid.NewGuid().ToString()); // Non-existent annotation

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationNotFound");
    }

    [Fact]
    public async Task Handle_WithInvalidStrand_ShouldFail()
    {
        // Arrange
        var (trace, annotationId) = CreateProcessedTraceWithAnnotation();
        SetupTraceRepository(trace);
        var command = CreateValidCommand(annotationId) with { StrandId = 999 };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidAnnotationStrand");
    }

    [Fact]
    public async Task Handle_WhenTraceNotFound_ShouldFail()
    {
        // Arrange
        _traceRepository.GetByIdWithAnnotationsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);
        var command = CreateValidCommand(Guid.NewGuid().ToString());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Helper Methods

    private UpdateAnnotationCommand CreateValidCommand(string annotationId)
    {
        return new UpdateAnnotationCommand(
            UserId: _validUserId,
            TraceId: _validTraceId,
            AnnotationId: annotationId,
            Label: "Updated Label",
            Description: "Updated description",
            StartPosition: 20,
            EndPosition: 150,
            StrandId: AnnotationStrand.Minus.Id,
            Color: "#FF5733",
            IsShared: true);
    }

    private void SetupTraceRepository(Trace trace)
    {
        _traceRepository.GetByIdWithAnnotationsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns(trace);
    }

    private Trace CreateProcessedTrace()
    {
        var traceId = TraceId.Parse(_validTraceId);
        var studyId = StudyId.FromSequence(1);
        var userId = UserId.Parse(_validUserId);
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/traces/sample.ab1", 1024, "abc123").Value;

        var trace = Trace.Create(traceId, studyId, userId, traceName, null, traceFile, TraceFormat.AB1).Value;
        trace.StartProcessing();
        trace.TransitionToProcessing();
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    private (Trace trace, string annotationId) CreateProcessedTraceWithAnnotation()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse(_validUserId);

        var annotationResult = trace.AddAnnotation(
            AnnotationType.Region,
            "Original Label",
            "Original description",
            10,
            100,
            AnnotationStrand.Plus,
            "#FF0000",
            false,
            null,
            userId);

        return (trace, annotationResult.Value.Id.ToString());
    }

    #endregion
}
