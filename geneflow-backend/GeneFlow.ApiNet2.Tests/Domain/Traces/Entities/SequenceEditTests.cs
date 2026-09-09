using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Entities;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Entities;

/// <summary>
/// Unit tests for SequenceEdit entity.
/// </summary>
public class SequenceEditTests
{
    private readonly UserId _userId = UserId.Parse("U00000001");

    #region Create - Change Type

    [Fact]
    public void Create_WithValidChangeEdit_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, 'A', 'G', "Test reason", _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var edit = result.Value;
        edit.EditType.Should().Be(EditType.Change);
        edit.Position.Should().Be(100);
        edit.OriginalBase.Should().Be('A');
        edit.NewBase.Should().Be('G');
        edit.Reason.Should().Be("Test reason");
        edit.EditedBy.Should().Be(_userId);
        edit.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ChangeEdit_WithoutOriginalBase_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, null, 'G', null, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("OriginalBaseRequired");
    }

    [Fact]
    public void Create_ChangeEdit_WithoutNewBase_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, 'A', null, null, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NewBaseRequired");
    }

    #endregion

    #region Create - Insert Type

    [Fact]
    public void Create_WithValidInsertEdit_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Insert, 200, null, 'T', "Missing base", _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var edit = result.Value;
        edit.EditType.Should().Be(EditType.Insert);
        edit.Position.Should().Be(200);
        edit.OriginalBase.Should().BeNull();
        edit.NewBase.Should().Be('T');
        edit.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_InsertEdit_WithoutNewBase_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Insert, 200, null, null, null, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NewBaseRequired");
    }

    #endregion

    #region Create - Delete Type

    [Fact]
    public void Create_WithValidDeleteEdit_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Delete, 300, 'C', null, "Extra base", _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var edit = result.Value;
        edit.EditType.Should().Be(EditType.Delete);
        edit.Position.Should().Be(300);
        edit.OriginalBase.Should().Be('C');
        edit.NewBase.Should().BeNull();
        edit.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_DeleteEdit_WithoutOriginalBase_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Delete, 300, null, null, null, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("OriginalBaseRequired");
    }

    #endregion

    #region Position Validation

    [Fact]
    public void Create_WithNegativePosition_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, -1, 'A', 'G', null, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPosition");
    }

    [Fact]
    public void Create_WithPositionEqualToSequenceLength_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace(); // Has 1000 bases (0-999 valid positions)

        // Act
        var result = trace.AddEdit(EditType.Change, 1000, 'A', 'G', null, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPosition");
    }

    [Fact]
    public void Create_WithPositionAtLastValidIndex_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace(); // Has 1000 bases (0-999 valid positions)

        // Act
        var result = trace.AddEdit(EditType.Change, 999, 'A', 'G', null, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Position.Should().Be(999);
    }

    #endregion

    #region Base Validation

    [Theory]
    [InlineData('A')]
    [InlineData('C')]
    [InlineData('G')]
    [InlineData('T')]
    [InlineData('N')]
    public void Create_WithValidBases_ShouldSucceed(char validBase)
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, validBase, 'A', null, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData('a')]
    [InlineData('c')]
    [InlineData('g')]
    [InlineData('t')]
    [InlineData('n')]
    public void Create_WithLowercaseBases_ShouldNormalizeToUppercase(char lowercaseBase)
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, lowercaseBase, 'A', null, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OriginalBase.Should().Be(char.ToUpperInvariant(lowercaseBase));
    }

    [Theory]
    [InlineData('X')]
    [InlineData('Z')]
    [InlineData('1')]
    [InlineData(' ')]
    public void Create_WithInvalidBases_ShouldFail(char invalidBase)
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, invalidBase, 'A', null, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidBase");
    }

    #endregion

    #region Reason Validation

    [Fact]
    public void Create_WithTooLongReason_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var longReason = new string('a', SequenceEdit.MaxReasonLength + 1);

        // Act
        var result = trace.AddEdit(EditType.Change, 100, 'A', 'G', longReason, _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ReasonTooLong");
    }

    [Fact]
    public void Create_WithMaxLengthReason_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var maxReason = new string('a', SequenceEdit.MaxReasonLength);

        // Act
        var result = trace.AddEdit(EditType.Change, 100, 'A', 'G', maxReason, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullReason_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result = trace.AddEdit(EditType.Change, 100, 'A', 'G', null, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Reason.Should().BeNull();
    }

    #endregion

    #region Undo

    [Fact]
    public void Undo_ActiveEdit_ShouldDeactivate()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var editResult = trace.AddEdit(EditType.Change, 100, 'A', 'G', null, _userId);
        var edit = editResult.Value;

        // Act
        var undoResult = trace.UndoEdit(edit.Id, _userId);

        // Assert
        undoResult.IsSuccess.Should().BeTrue();
        edit.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Undo_AlreadyUndoneEdit_ShouldFail()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var editResult = trace.AddEdit(EditType.Change, 100, 'A', 'G', null, _userId);
        var edit = editResult.Value;
        trace.UndoEdit(edit.Id, _userId);

        // Act
        var secondUndoResult = trace.UndoEdit(edit.Id, _userId);

        // Assert
        secondUndoResult.IsFailure.Should().BeTrue();
        secondUndoResult.Error.Code.Should().Contain("EditAlreadyUndone");
    }

    #endregion

    #region Helper Methods

    private Trace CreateUploadedTrace()
    {
        var traceId = TraceId.New();
        var studyId = StudyId.FromSequence(1);
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceDescription = TraceDescription.Create("Test").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/path", 1024, "checksum").Value;

        return Trace.Create(traceId, studyId, _userId, traceName, traceDescription, traceFile, TraceFormat.AB1).Value;
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
        var metrics = QualityMetrics.Create(35, 1000, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    #endregion
}
