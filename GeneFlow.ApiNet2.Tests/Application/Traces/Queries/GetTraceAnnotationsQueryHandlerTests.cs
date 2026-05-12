using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceAnnotations;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Traces.Queries;

/// <summary>
/// Unit tests for GetTraceAnnotationsQueryHandler.
/// </summary>
public class GetTraceAnnotationsQueryHandlerTests
{
    private readonly ITraceRepository _traceRepository = Substitute.For<ITraceRepository>();
    private readonly GetTraceAnnotationsQueryHandler _handler;

    private readonly string _validUserId = "U00000001";
    private readonly string _validTraceId = Guid.NewGuid().ToString();

    public GetTraceAnnotationsQueryHandlerTests()
    {
        _handler = new GetTraceAnnotationsQueryHandler(_traceRepository);
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidTraceId_ShouldReturnAnnotations()
    {
        // Arrange
        var trace = CreateProcessedTraceWithAnnotations();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoAnnotations_ShouldReturnEmptyList()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnAnnotationDetails()
    {
        // Arrange
        var trace = CreateProcessedTraceWithAnnotations();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var firstAnnotation = result.Value.First();
        firstAnnotation.Label.Should().NotBeNullOrEmpty();
        firstAnnotation.Type.Should().NotBeNullOrEmpty();
        firstAnnotation.Strand.Should().NotBeNullOrEmpty();
        firstAnnotation.Color.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnAnnotationsOrderedByPosition()
    {
        // Arrange
        var trace = CreateProcessedTraceWithAnnotations();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var positions = result.Value.Select(a => a.StartPosition).ToList();
        positions.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_ShouldIncludeSharedAndPrivateAnnotations()
    {
        // Arrange
        var trace = CreateProcessedTraceWithMixedAnnotations();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(a => a.IsShared == true);
        result.Value.Should().Contain(a => a.IsShared == false);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectTraceId()
    {
        // Arrange
        var trace = CreateProcessedTraceWithAnnotations();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        foreach (var annotation in result.Value)
        {
            annotation.TraceId.Should().Be(_validTraceId);
        }
    }

    [Fact]
    public async Task Handle_ShouldReturnAnnotationsWithDifferentTypes()
    {
        // Arrange
        var trace = CreateProcessedTraceWithDifferentAnnotationTypes();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(a => a.Type).Distinct().Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnAnnotationsWithDifferentStrands()
    {
        // Arrange
        var trace = CreateProcessedTraceWithDifferentStrands();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Select(a => a.Strand).Distinct().Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidTraceId_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetTraceAnnotationsQuery("invalid-trace-id");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentTrace_ShouldReturnNotFound()
    {
        // Arrange
        _traceRepository.GetByIdWithAnnotationsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
            .Returns((Trace?)null);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithEmptyTraceId_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetTraceAnnotationsQuery(string.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task Handle_WithNullGuidTraceId_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetTraceAnnotationsQuery(Guid.Empty.ToString());

        _traceRepository.GetByIdWithAnnotationsAsync(Arg.Any<TraceId>(), Arg.Any<CancellationToken>())
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
    public async Task Handle_ShouldCallRepositoryWithCorrectTraceId()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        await _traceRepository.Received(1).GetByIdWithAnnotationsAsync(
            Arg.Any<TraceId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldUseCancellationToken()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        SetupRepository(trace);
        var query = new GetTraceAnnotationsQuery(_validTraceId);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(query, token);

        // Assert
        await _traceRepository.Received(1).GetByIdWithAnnotationsAsync(
            Arg.Any<TraceId>(),
            token);
    }

    #endregion

    #region Helper Methods

    private void SetupRepository(Trace trace)
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

    private Trace CreateProcessedTraceWithAnnotations()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse(_validUserId);

        // Add first annotation at position 10
        trace.AddAnnotation(
            AnnotationType.Region,
            "First Annotation",
            "First description",
            10,
            100,
            AnnotationStrand.Plus,
            "#FF0000",
            false,
            null,
            userId);

        // Add second annotation at position 200
        trace.AddAnnotation(
            AnnotationType.Region,
            "Second Annotation",
            "Second description",
            200,
            300,
            AnnotationStrand.Minus,
            "#00FF00",
            false,
            null,
            userId);

        return trace;
    }

    private Trace CreateProcessedTraceWithMixedAnnotations()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse(_validUserId);

        // Add shared annotation
        trace.AddAnnotation(
            AnnotationType.Region,
            "Shared Annotation",
            "Shared description",
            10,
            100,
            AnnotationStrand.Plus,
            "#FF0000",
            true,  // shared
            null,
            userId);

        // Add private annotation
        trace.AddAnnotation(
            AnnotationType.Point,
            "Private Annotation",
            "Private description",
            200,
            200,
            AnnotationStrand.Minus,
            "#00FF00",
            false,  // not shared
            null,
            userId);

        return trace;
    }

    private Trace CreateProcessedTraceWithDifferentAnnotationTypes()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse(_validUserId);

        // Add Region annotation
        trace.AddAnnotation(
            AnnotationType.Region,
            "Region Annotation",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "#FF0000",
            false,
            null,
            userId);

        // Add Point annotation
        trace.AddAnnotation(
            AnnotationType.Point,
            "Point Annotation",
            null,
            150,
            150,
            AnnotationStrand.Plus,
            "#00FF00",
            false,
            null,
            userId);

        // Add Feature annotation
        trace.AddAnnotation(
            AnnotationType.Feature,
            "Feature Annotation",
            null,
            200,
            220,
            AnnotationStrand.Plus,
            "#0000FF",
            false,
            null,
            userId);

        return trace;
    }

    private Trace CreateProcessedTraceWithDifferentStrands()
    {
        var trace = CreateProcessedTrace();
        var userId = UserId.Parse(_validUserId);

        // Add Plus strand annotation
        trace.AddAnnotation(
            AnnotationType.Region,
            "Plus Strand",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "#FF0000",
            false,
            null,
            userId);

        // Add Minus strand annotation
        trace.AddAnnotation(
            AnnotationType.Region,
            "Minus Strand",
            null,
            200,
            300,
            AnnotationStrand.Minus,
            "#00FF00",
            false,
            null,
            userId);

        // Add None strand annotation
        trace.AddAnnotation(
            AnnotationType.Region,
            "No Strand",
            null,
            400,
            500,
            AnnotationStrand.None,
            "#0000FF",
            false,
            null,
            userId);

        return trace;
    }

    #endregion
}
