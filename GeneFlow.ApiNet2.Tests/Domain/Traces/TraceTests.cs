using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.Events;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces;

/// <summary>
/// Unit tests for Trace aggregate root.
/// </summary>
public class TraceTests
{
    private readonly TraceId _traceId = TraceId.New();
    private readonly StudyId _studyId = StudyId.FromSequence(1);
    private readonly UserId _userId = UserId.Parse("U00000001");
    private readonly TraceName _traceName = TraceName.Create("Sample_001.ab1").Value;
    private readonly TraceDescription _traceDescription = TraceDescription.Create("Test description").Value;
    private readonly TraceFile _traceFile;

    public TraceTests()
    {
        _traceFile = TraceFile.Create(
            "sample.ab1",
            "application/octet-stream",
            "/traces/sample.ab1",
            1024,
            "abc123").Value;
    }

    #region Factory Method

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Act
        var result = Trace.Create(
            _traceId,
            _studyId,
            _userId,
            _traceName,
            _traceDescription,
            _traceFile,
            TraceFormat.AB1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(_traceId);
        result.Value.StudyId.Should().Be(_studyId);
        result.Value.UploadedBy.Should().Be(_userId);
        result.Value.Name.Should().Be(_traceName);
        result.Value.Description.Should().Be(_traceDescription);
        result.Value.File.Should().Be(_traceFile);
        result.Value.Format.Should().Be(TraceFormat.AB1);
        result.Value.Status.Should().Be(TraceStatus.Uploaded);
    }

    [Fact]
    public void Create_ShouldSetHasChromatogramData_FromFormat()
    {
        // Act - AB1 format has chromatogram data
        var ab1Result = Trace.Create(_traceId, _studyId, _userId, _traceName, _traceDescription, _traceFile, TraceFormat.AB1);

        // Assert
        ab1Result.Value.HasChromatogramData.Should().BeTrue();

        // Act - FASTA format does not have chromatogram data
        var fastaResult = Trace.Create(TraceId.New(), _studyId, _userId, _traceName, _traceDescription, _traceFile, TraceFormat.FASTA);

        // Assert
        fastaResult.Value.HasChromatogramData.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldRaiseTraceUploadedEvent()
    {
        // Act
        var result = Trace.Create(_traceId, _studyId, _userId, _traceName, _traceDescription, _traceFile, TraceFormat.AB1);

        // Assert
        result.Value.DomainEvents.Should().ContainSingle(e => e is TraceUploadedEvent);
        var evt = result.Value.DomainEvents.OfType<TraceUploadedEvent>().First();
        evt.TraceId.Should().Be(_traceId);
        evt.StudyId.Should().Be(_studyId);
    }

    #endregion

    #region Update Name

    [Fact]
    public void UpdateName_ShouldUpdateName()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var newName = TraceName.Create("Updated_Name.ab1").Value;

        // Act
        var result = trace.UpdateName(newName, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Name.Should().Be(newName);
    }

    #endregion

    #region Update Description

    [Fact]
    public void UpdateDescription_ShouldUpdateDescription()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var newDescription = TraceDescription.Create("New description").Value;

        // Act
        var result = trace.UpdateDescription(newDescription, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Description.Should().Be(newDescription);
    }

    #endregion

    #region Status Transitions

    [Fact]
    public void StartProcessing_FromUploaded_ShouldTransitionToValidating()
    {
        // Arrange
        var trace = CreateUploadedTrace();

        // Act
        var result = trace.StartProcessing();

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Status.Should().Be(TraceStatus.Validating);
    }

    [Fact]
    public void StartProcessing_FromProcessed_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.StartProcessing();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    [Fact]
    public void TransitionToProcessing_FromValidating_ShouldSucceed()
    {
        // Arrange
        var trace = CreateUploadedTrace();
        trace.StartProcessing();

        // Act
        var result = trace.TransitionToProcessing();

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Status.Should().Be(TraceStatus.Processing);
    }

    [Fact]
    public void CompleteProcessing_WithValidMetrics_ShouldTransitionToProcessed()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var metrics = CreateQualityMetrics();

        // Act
        var result = trace.CompleteProcessing(metrics, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Status.Should().Be(TraceStatus.Processed);
        trace.QualityMetrics.Should().Be(metrics);
        trace.HasChromatogramData.Should().BeTrue();
        trace.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void CompleteProcessing_ShouldRaiseTraceProcessedEvent()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var metrics = CreateQualityMetrics();
        trace.ClearDomainEvents();

        // Act
        trace.CompleteProcessing(metrics, true);

        // Assert
        trace.DomainEvents.Should().ContainSingle(e => e is TraceProcessedEvent);
    }

    [Fact]
    public void FailProcessing_WithReason_ShouldTransitionToFailed()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var reason = "Invalid file format";

        // Act
        var result = trace.FailProcessing(reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Status.Should().Be(TraceStatus.Failed);
        trace.FailureReason.Should().Be(reason);
    }

    [Fact]
    public void FailProcessing_WithEmptyReason_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessingTrace();

        // Act
        var result = trace.FailProcessing("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FailureReasonRequired");
    }

    [Fact]
    public void FailProcessing_WithTooLongReason_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessingTrace();
        var longReason = new string('a', Trace.MaxFailureReasonLength + 1);

        // Act
        var result = trace.FailProcessing(longReason);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FailureReasonTooLong");
    }

    [Fact]
    public void Retry_FromFailed_ShouldTransitionToUploaded()
    {
        // Arrange
        var trace = CreateFailedTrace();

        // Act
        var result = trace.Retry();

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Status.Should().Be(TraceStatus.Uploaded);
        trace.FailureReason.Should().BeNull();
    }

    [Fact]
    public void Retry_FromProcessed_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.Retry();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotRetryNotFailed");
    }

    [Fact]
    public void Archive_FromProcessed_ShouldTransitionToArchived()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.Archive(_userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Status.Should().Be(TraceStatus.Archived);
    }

    [Fact]
    public void Archive_FromProcessing_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessingTrace();

        // Act
        var result = trace.Archive(_userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStatusTransition");
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void IsProcessed_WhenProcessed_ShouldBeTrue()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Assert
        trace.IsProcessed.Should().BeTrue();
        trace.IsFailed.Should().BeFalse();
        trace.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void IsFailed_WhenFailed_ShouldBeTrue()
    {
        // Arrange
        var trace = CreateFailedTrace();

        // Assert
        trace.IsFailed.Should().BeTrue();
        trace.IsProcessed.Should().BeFalse();
    }

    [Fact]
    public void CanBeEdited_WhenProcessed_ShouldBeTrue()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Assert
        trace.CanBeEdited.Should().BeTrue();
    }

    [Fact]
    public void CanBeEdited_WhenUploaded_ShouldBeFalse()
    {
        // Arrange
        var trace = CreateUploadedTrace();

        // Assert
        trace.CanBeEdited.Should().BeFalse();
    }

    [Fact]
    public void TotalBases_WithMetrics_ShouldReturnCorrectValue()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Assert
        trace.TotalBases.Should().Be(1000);
    }

    [Fact]
    public void TotalBases_WithoutMetrics_ShouldReturnZero()
    {
        // Arrange
        var trace = CreateUploadedTrace();

        // Assert
        trace.TotalBases.Should().Be(0);
    }

    #endregion

    #region Trimming (Multiple Trims)

    [Fact]
    public void AddTrim_WhenProcessed_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId, "Low quality");

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Trims.Should().HaveCount(1);
        trace.ActiveTrimCount.Should().Be(1);
        result.Value.StartPosition.Should().Be(0);
        result.Value.EndPosition.Should().Be(50);
    }

    [Fact]
    public void AddTrim_WhenNotProcessed_ShouldFail()
    {
        // Arrange
        var trace = CreateUploadedTrace();

        // Act
        var result = trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotEditInCurrentStatus");
    }

    [Fact]
    public void AddTrim_MultipleTrims_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act - Add 5' trim
        var result1 = trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId, null);
        // Add 3' trim
        var result2 = trace.AddTrim(TrimType.Manual, 950, 1000, TrimEnd.ThreePrime, "Manual", _userId, null);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        trace.Trims.Should().HaveCount(2);
        trace.ActiveTrimCount.Should().Be(2);
    }

    [Fact]
    public void AddTrim_WithInvalidPositions_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act - start >= end
        var result = trace.AddTrim(TrimType.Manual, 100, 50, TrimEnd.FivePrime, "Manual", _userId, null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPositions");
    }

    [Fact]
    public void UndoTrim_SpecificTrim_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var trim1 = trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId, null).Value;
        var trim2 = trace.AddTrim(TrimType.Manual, 950, 1000, TrimEnd.ThreePrime, "Manual", _userId, null).Value;

        // Act - Undo only the first trim
        var result = trace.UndoTrim(trim1.Id, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveTrimCount.Should().Be(1);
        trace.GetActiveTrims().Should().Contain(t => t.Id == trim2.Id);
    }

    [Fact]
    public void UndoTrim_NonExistentTrim_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.UndoTrim(Guid.NewGuid(), _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimNotFound");
    }

    [Fact]
    public void UndoAllTrims_ShouldDeactivateAllTrims()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId, null);
        trace.AddTrim(TrimType.Manual, 950, 1000, TrimEnd.ThreePrime, "Manual", _userId, null);

        // Act
        var result = trace.UndoAllTrims(_userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveTrimCount.Should().Be(0);
    }

    [Fact]
    public void UndoAllTrims_WithNoActiveTrims_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.UndoAllTrims(_userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoActiveTrims");
    }

    [Fact]
    public void GetActiveTrims_ShouldReturnOnlyActiveTrims()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var trim1 = trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId, null).Value;
        var trim2 = trace.AddTrim(TrimType.Manual, 950, 1000, TrimEnd.ThreePrime, "Manual", _userId, null).Value;
        trace.UndoTrim(trim1.Id, _userId);

        // Act
        var activeTrims = trace.GetActiveTrims();

        // Assert
        activeTrims.Should().HaveCount(1);
        activeTrims.Should().Contain(trim2);
    }

    #endregion

    #region Sequence Editing

    [Fact]
    public void AddEdit_WhenProcessed_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, 'A', 'G', "Correction", _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.Edits.Should().HaveCount(1);
        trace.ActiveEditCount.Should().Be(1);
    }

    [Fact]
    public void AddEdit_WhenNotProcessed_ShouldFail()
    {
        // Arrange
        var trace = CreateUploadedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, 'A', 'G', null, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("CannotEditInCurrentStatus");
    }

    [Fact]
    public void UndoEdit_ShouldDeactivateEdit()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var editResult = trace.AddEdit(EditType.Change, 100, 'A', 'G', null, _userId);
        var editId = editResult.Value.Id;

        // Act
        var result = trace.UndoEdit(editId, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveEditCount.Should().Be(0);
    }

    [Fact]
    public void UndoEdit_WithNonExistentId_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.UndoEdit(Guid.NewGuid(), _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EditNotFound");
    }

    [Fact]
    public void UndoAllEdits_ShouldDeactivateAllEdits()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        trace.AddEdit(EditType.Change, 100, 'A', 'G', null, _userId);
        trace.AddEdit(EditType.Insert, 200, null, 'T', null, _userId);
        trace.AddEdit(EditType.Delete, 300, 'C', null, null, _userId);

        // Act
        var result = trace.UndoAllEdits(_userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveEditCount.Should().Be(0);
    }

    [Fact]
    public void UndoAllEdits_WithNoActiveEdits_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.UndoAllEdits(_userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NoActiveEdits");
    }

    [Fact]
    public void GetActiveEdits_ShouldReturnOnlyActiveEdits()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var edit1 = trace.AddEdit(EditType.Change, 100, 'A', 'G', null, _userId).Value;
        var edit2 = trace.AddEdit(EditType.Insert, 200, null, 'T', null, _userId).Value;
        trace.UndoEdit(edit1.Id, _userId);

        // Act
        var activeEdits = trace.GetActiveEdits();

        // Assert
        activeEdits.Should().HaveCount(1);
        activeEdits.Should().Contain(edit2);
    }

    #endregion

    #region Helper Methods

    private Trace CreateUploadedTrace()
    {
        return Trace.Create(
            _traceId,
            _studyId,
            _userId,
            _traceName,
            _traceDescription,
            _traceFile,
            TraceFormat.AB1).Value;
    }

    private Trace CreateProcessingTrace()
    {
        var trace = CreateUploadedTrace();
        trace.StartProcessing();
        trace.TransitionToProcessing();
        return trace;
    }

    private Trace CreateProcessedTrace()
    {
        var trace = CreateProcessingTrace();
        var metrics = CreateQualityMetrics();
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    private Trace CreateFailedTrace()
    {
        var trace = CreateProcessingTrace();
        trace.FailProcessing("Test failure reason");
        return trace;
    }

    private QualityMetrics CreateQualityMetrics()
    {
        return QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
    }

    #endregion
}
