using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Entities;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Entities;

/// <summary>
/// Unit tests for TraceTrim entity.
/// Tests creation, validation, activation state, and trim operations.
/// </summary>
public class TraceTrimTests
{
    private readonly UserId _userId = UserId.Parse("U00000001");
    private const int SequenceLength = 1000;

    #region Create - Auto Trim

    [Fact]
    public void Create_AutoTrim_ShouldCreate()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.AutoMott,
            0,
            50,
            TrimEnd.FivePrime,
            "Mott(Q20,W10)",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var trim = result.Value;
        trim.TrimType.Should().Be(TrimType.AutoMott);
        trim.StartPosition.Should().Be(0);
        trim.EndPosition.Should().Be(50);
        trim.TrimEnd.Should().Be(TrimEnd.FivePrime);
        trim.Algorithm.Should().Be("Mott(Q20,W10)");
        trim.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_AutoMott_ShouldSetCorrectTrimType()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.AutoMott,
            0,
            30,
            TrimEnd.FivePrime,
            "Mott(Q20,W10)",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrimType.Should().Be(TrimType.AutoMott);
        result.Value.TrimType.IsAutomatic.Should().BeTrue();
    }

    [Fact]
    public void Create_AutoWindow_ShouldSetCorrectTrimType()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.AutoWindow,
            970,
            1000,
            TrimEnd.ThreePrime,
            "Window(Q15,W20)",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrimType.Should().Be(TrimType.AutoWindow);
        result.Value.TrimType.IsAutomatic.Should().BeTrue();
    }

    #endregion

    #region Create - Manual Trim

    [Fact]
    public void Create_ManualTrim_ShouldCreate()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            100,
            200,
            TrimEnd.FivePrime,
            "Manual",
            _userId,
            "Low quality region");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var trim = result.Value;
        trim.TrimType.Should().Be(TrimType.Manual);
        trim.Reason.Should().Be("Low quality region");
        trim.TrimType.IsAutomatic.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldSetAppliedByAndAppliedAt()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = trace.AddTrim(
            TrimType.Manual,
            0,
            50,
            TrimEnd.FivePrime,
            "Manual",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var trim = result.Value;
        trim.AppliedBy.Should().Be(_userId);
        trim.AppliedAt.Should().BeAfter(beforeCreate);
        trim.AppliedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region Create - TrimEnd Variations

    [Fact]
    public void Create_FivePrimeTrim_ShouldSetCorrectTrimEnd()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            0,
            50,
            TrimEnd.FivePrime,
            "Manual",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrimEnd.Should().Be(TrimEnd.FivePrime);
        result.Value.TrimEnd.DisplayName.Should().Be("5' End");
    }

    [Fact]
    public void Create_ThreePrimeTrim_ShouldSetCorrectTrimEnd()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            950,
            1000,
            TrimEnd.ThreePrime,
            "Manual",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrimEnd.Should().Be(TrimEnd.ThreePrime);
        result.Value.TrimEnd.DisplayName.Should().Be("3' End");
    }

    #endregion

    #region Create - Validation Failures

    [Fact]
    public void Create_NegativeStartPosition_ShouldReturnError()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            -10,
            50,
            TrimEnd.FivePrime,
            "Manual",
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPosition");
    }

    [Fact]
    public void Create_EndBeforeStart_ShouldReturnError()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            100,
            50, // End before start
            TrimEnd.FivePrime,
            "Manual",
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPositions");
    }

    [Fact]
    public void Create_EndEqualsStart_ShouldReturnError()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            100,
            100, // End equals start (zero length)
            TrimEnd.FivePrime,
            "Manual",
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPositions");
    }

    [Fact]
    public void Create_ExceedsSequenceLength_ShouldReturnError()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace(); // 1000 bases
        var result = trace.AddTrim(
            TrimType.Manual,
            950,
            1100, // Exceeds sequence length
            TrimEnd.ThreePrime,
            "Manual",
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimExceedsSequenceLength");
    }

    [Fact]
    public void Create_EmptyAlgorithm_ShouldReturnError()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            0,
            50,
            TrimEnd.FivePrime,
            "", // Empty algorithm
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimAlgorithmRequired");
    }

    [Fact]
    public void Create_AlgorithmTooLong_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var longAlgorithm = new string('x', TraceTrim.MaxAlgorithmLength + 1);

        // Act
        var result = trace.AddTrim(
            TrimType.Manual,
            0,
            50,
            TrimEnd.FivePrime,
            longAlgorithm,
            _userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimAlgorithmTooLong");
    }

    [Fact]
    public void Create_ReasonTooLong_ShouldReturnError()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var longReason = new string('x', TraceTrim.MaxReasonLength + 1);

        // Act
        var result = trace.AddTrim(
            TrimType.Manual,
            0,
            50,
            TrimEnd.FivePrime,
            "Manual",
            _userId,
            longReason);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimReasonTooLong");
    }

    #endregion

    #region Length Property

    [Fact]
    public void Length_ShouldReturnCorrectValue()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            100,
            250,
            TrimEnd.FivePrime,
            "Manual",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Length.Should().Be(150);
    }

    [Theory]
    [InlineData(0, 50, 50)]
    [InlineData(100, 200, 100)]
    [InlineData(950, 1000, 50)]
    [InlineData(0, 1, 1)]
    public void Length_VariousPositions_ShouldCalculateCorrectly(int start, int end, int expectedLength)
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            start,
            end,
            TrimEnd.FivePrime,
            "Manual",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Length.Should().Be(expectedLength);
    }

    #endregion

    #region IsActive and Deactivate

    [Fact]
    public void IsActive_NewTrim_ShouldBeTrue()
    {
        // Arrange & Act
        var trace = CreateProcessedTrace();
        var result = trace.AddTrim(
            TrimType.Manual,
            0,
            50,
            TrimEnd.FivePrime,
            "Manual",
            _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UndoTrim_ShouldDeactivateTrim()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var trimResult = trace.AddTrim(
            TrimType.Manual,
            0,
            50,
            TrimEnd.FivePrime,
            "Manual",
            _userId);
        var trim = trimResult.Value;

        // Act
        var undoResult = trace.UndoTrim(trim.Id, _userId);

        // Assert
        undoResult.IsSuccess.Should().BeTrue();
        trim.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UndoTrim_DeactivatedTrim_ShouldNotAppearInActiveTrims()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        var trim1 = trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId).Value;
        var trim2 = trace.AddTrim(TrimType.Manual, 950, 1000, TrimEnd.ThreePrime, "Manual", _userId).Value;

        // Act
        trace.UndoTrim(trim1.Id, _userId);

        // Assert
        trace.GetActiveTrims().Should().HaveCount(1);
        trace.GetActiveTrims().Should().Contain(trim2);
        trace.GetActiveTrims().Should().NotContain(trim1);
    }

    #endregion

    #region Multiple Trims

    [Fact]
    public void AddTrim_MultipleTrimsSameTrace_ShouldSucceed()
    {
        // Arrange
        var trace = CreateProcessedTrace();

        // Act
        var result1 = trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId);
        var result2 = trace.AddTrim(TrimType.Manual, 950, 1000, TrimEnd.ThreePrime, "Manual", _userId);
        var result3 = trace.AddTrim(TrimType.AutoMott, 50, 100, TrimEnd.FivePrime, "Mott", _userId);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        trace.Trims.Should().HaveCount(3);
        trace.ActiveTrimCount.Should().Be(3);
    }

    [Fact]
    public void UndoAllTrims_ShouldDeactivateAll()
    {
        // Arrange
        var trace = CreateProcessedTrace();
        trace.AddTrim(TrimType.Manual, 0, 50, TrimEnd.FivePrime, "Manual", _userId);
        trace.AddTrim(TrimType.Manual, 950, 1000, TrimEnd.ThreePrime, "Manual", _userId);
        trace.ActiveTrimCount.Should().Be(2);

        // Act
        var result = trace.UndoAllTrims(_userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        trace.ActiveTrimCount.Should().Be(0);
        trace.Trims.Should().HaveCount(2); // Trims still exist, just deactivated
    }

    #endregion

    #region Helper Methods

    private Trace CreateProcessedTrace()
    {
        var traceId = TraceId.New();
        var studyId = StudyId.FromSequence(1);
        var traceName = TraceName.Create("Sample_001.ab1").Value;
        var traceDescription = TraceDescription.Create("Test").Value;
        var traceFile = TraceFile.Create("sample.ab1", "application/octet-stream", "/path", 1024, "checksum").Value;

        var trace = Trace.Create(traceId, studyId, _userId, traceName, traceDescription, traceFile, TraceFormat.AB1).Value;
        trace.StartProcessing();
        trace.TransitionToProcessing();
        var metrics = QualityMetrics.Create(35, SequenceLength, 90, 80, 900, 45).Value;
        trace.CompleteProcessing(metrics, true);
        return trace;
    }

    #endregion
}
