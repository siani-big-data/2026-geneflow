using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Entities;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Entities;

/// <summary>
/// Unit tests for TraceAnnotation entity.
/// Tests annotation creation and update through the Trace aggregate.
/// </summary>
public class TraceAnnotationTests
{
    private readonly UserId _userId = UserId.Parse("U00000001");
    private readonly StudyId _studyId = StudyId.FromSequence(1);
    private const int SequenceLength = 1000;

    #region Create - Valid Cases

    [Fact]
    public void AddAnnotation_WithValidData_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test Annotation",
            "Test description",
            10,
            100,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Label.Should().Be("Test Annotation");
        result.Value.Description.Should().Be("Test description");
        result.Value.StartPosition.Should().Be(10);
        result.Value.EndPosition.Should().Be(100);
        result.Value.Type.Should().Be(AnnotationType.Region);
        result.Value.Strand.Should().Be(AnnotationStrand.Plus);
        result.Value.Color.Should().Be("#FF5733");
        result.Value.IsShared.Should().BeFalse();
    }

    [Fact]
    public void AddAnnotation_WithNullDescription_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test Annotation",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public void AddAnnotation_WithEmptyColor_ShouldUseDefaultGray()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test Annotation",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "",
            false,
            null,
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Color.Should().Be("#808080");
    }

    [Fact]
    public void AddAnnotation_ShouldSetCreationAudit()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test Annotation",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.CreatedBy.Should().Be(_userId.Value.ToString());
    }

    [Fact]
    public void AddAnnotation_ShouldGenerateUniqueId()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result1 = trace.AddAnnotation(
            AnnotationType.Region, "Test 1", null, 10, 100,
            AnnotationStrand.Plus, "#FF5733", false, null, _userId);

        var result2 = trace.AddAnnotation(
            AnnotationType.Region, "Test 2", null, 10, 100,
            AnnotationStrand.Plus, "#FF5733", false, null, _userId);

        // Assert
        result1.Value.Id.Should().NotBe(result2.Value.Id);
    }

    #endregion

    #region Create - Position Validation

    [Fact]
    public void AddAnnotation_WithNegativeStartPosition_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test Annotation",
            null,
            -1,
            100,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationOutOfBounds");
    }

    [Fact]
    public void AddAnnotation_WithStartPositionBeyondSequence_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test Annotation",
            null,
            1500,
            1600,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationOutOfBounds");
    }

    [Fact]
    public void AddAnnotation_WithEndPositionBeyondSequence_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test Annotation",
            null,
            10,
            1500,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationOutOfBounds");
    }

    [Fact]
    public void AddAnnotation_WithEndBeforeStart_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test Annotation",
            null,
            100,
            50,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidAnnotationRange");
    }

    #endregion

    #region Create - Point Annotation Type

    [Fact]
    public void AddAnnotation_PointAnnotation_ShouldNormalizeEndToStart()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Point,
            "Point Annotation",
            null,
            50,
            100,  // Different from start, should be normalized
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StartPosition.Should().Be(50);
        result.Value.EndPosition.Should().Be(50);
    }

    [Fact]
    public void AddAnnotation_PointAnnotation_WithSameStartAndEnd_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Point,
            "Point Annotation",
            null,
            50,
            50,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Length.Should().Be(1);
    }

    #endregion

    #region Create - Label Validation

    [Fact]
    public void AddAnnotation_WithEmptyLabel_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationLabelRequired");
    }

    [Fact]
    public void AddAnnotation_WithWhitespaceOnlyLabel_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "   ",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationLabelRequired");
    }

    [Fact]
    public void AddAnnotation_WithLabelTooLong_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var longLabel = new string('a', TraceAnnotation.MaxLabelLength + 1);

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            longLabel,
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationLabelTooLong");
    }

    [Fact]
    public void AddAnnotation_WithDescriptionTooLong_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var longDescription = new string('a', TraceAnnotation.MaxDescriptionLength + 1);

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test",
            longDescription,
            10,
            100,
            AnnotationStrand.Plus,
            "#FF5733",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationDescriptionTooLong");
    }

    #endregion

    #region Create - Color Validation

    [Fact]
    public void AddAnnotation_WithInvalidColorFormat_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "red",  // Invalid - not hex format
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidColor");
    }

    [Fact]
    public void AddAnnotation_WithShortHexColor_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddAnnotation(
            AnnotationType.Region,
            "Test",
            null,
            10,
            100,
            AnnotationStrand.Plus,
            "#FFF",  // Invalid - short format not supported
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidColor");
    }

    #endregion

    #region Update

    [Fact]
    public void UpdateAnnotation_WithValidData_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var annotation = trace.AddAnnotation(
            AnnotationType.Region, "Original", "Original desc", 10, 100,
            AnnotationStrand.Plus, "#FF5733", false, null, _userId).Value;

        // Act
        var result = trace.UpdateAnnotation(
            annotation.Id,
            "Updated Label",
            "Updated description",
            20,
            200,
            AnnotationStrand.Minus,
            "#00FF00",
            true,
            null,
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updated = trace.GetAnnotation(annotation.Id);
        updated!.Label.Should().Be("Updated Label");
        updated.Description.Should().Be("Updated description");
        updated.StartPosition.Should().Be(20);
        updated.EndPosition.Should().Be(200);
        updated.Strand.Should().Be(AnnotationStrand.Minus);
        updated.Color.Should().Be("#00FF00");
        updated.IsShared.Should().BeTrue();
    }

    [Fact]
    public void UpdateAnnotation_ShouldSetModificationAudit()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var annotation = trace.AddAnnotation(
            AnnotationType.Region, "Original", null, 10, 100,
            AnnotationStrand.Plus, "#FF5733", false, null, _userId).Value;
        var updatingUserId = UserId.Parse("U00000002");

        // Act
        var result = trace.UpdateAnnotation(
            annotation.Id,
            "Updated Label",
            null,
            20,
            200,
            AnnotationStrand.Plus,
            "#00FF00",
            false,
            null,
            updatingUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updated = trace.GetAnnotation(annotation.Id);
        updated!.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updated.ModifiedBy.Should().Be(updatingUserId.Value.ToString());
    }

    [Fact]
    public void UpdateAnnotation_WithInvalidPosition_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var annotation = trace.AddAnnotation(
            AnnotationType.Region, "Original", null, 10, 100,
            AnnotationStrand.Plus, "#FF5733", false, null, _userId).Value;

        // Act
        var result = trace.UpdateAnnotation(
            annotation.Id,
            "Updated Label",
            null,
            -1,  // Invalid
            200,
            AnnotationStrand.Plus,
            "#00FF00",
            false,
            null,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AnnotationOutOfBounds");
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void Length_ShouldReturnCorrectValue()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var annotation = trace.AddAnnotation(
            AnnotationType.Region, "Test", null, 10, 100,
            AnnotationStrand.Plus, "#FF5733", false, null, _userId).Value;

        // Assert
        annotation.Length.Should().Be(91); // EndPosition - StartPosition + 1
    }

    #endregion

    #region Helper Methods

    private Trace CreateProcessedTrace()
    {
        var traceId = TraceId.New();
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/traces/sample.ab1", 1024, "abc123").Value;

        var trace = Trace.Create(traceId, _studyId, _userId, traceName, null, traceFile, TraceFormat.AB1).Value;
        trace.StartProcessing();
        trace.TransitionToProcessing();
        var metrics = QualityMetrics.Create(35, SequenceLength, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    #endregion
}
