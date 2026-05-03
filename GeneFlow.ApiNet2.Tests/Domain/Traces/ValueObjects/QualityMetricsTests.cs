using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.ValueObjects;

/// <summary>
/// Unit tests for QualityMetrics value object.
/// </summary>
public class QualityMetricsTests
{
    #region Create - Success Cases

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var avgScore = 35.5m;
        var totalBases = 1000;
        var q20 = 95.0m;
        var q30 = 85.0m;
        var trimmedLength = 900;
        var gcContent = 45.5m;

        // Act
        var result = QualityMetrics.Create(avgScore, totalBases, q20, q30, trimmedLength, gcContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AverageQualityScore.Should().Be(avgScore);
        result.Value.TotalBases.Should().Be(totalBases);
        result.Value.QualityAboveQ20Percentage.Should().Be(q20);
        result.Value.QualityAboveQ30Percentage.Should().Be(q30);
        result.Value.TrimmedLength.Should().Be(trimmedLength);
        result.Value.GcContentPercentage.Should().Be(gcContent);
    }

    [Fact]
    public void Create_WithZeroValues_ShouldSucceed()
    {
        // Act
        var result = QualityMetrics.Create(0, 0, 0, 0, 0, 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMaxPercentages_ShouldSucceed()
    {
        // Act
        var result = QualityMetrics.Create(50, 1000, 100, 100, 1000, 100);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithTrimmedLengthEqualToTotal_ShouldSucceed()
    {
        // Act
        var result = QualityMetrics.Create(30, 500, 80, 70, 500, 45);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrimmedLength.Should().Be(result.Value.TotalBases);
    }

    #endregion

    #region Create - Failure Cases

    [Fact]
    public void Create_WithNegativeQualityScoREDACTED()
    {
        // Act
        var result = QualityMetrics.Create(-1, 1000, 80, 70, 900, 45);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidQualityScore");
    }

    [Fact]
    public void Create_WithNegativeTotalBases_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, -1, 80, 70, 0, 45);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTotalBases");
    }

    [Fact]
    public void Create_WithNegativeQ20Percentage_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, 1000, -1, 70, 900, 45);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPercentage");
    }

    [Fact]
    public void Create_WithQ20PercentageOver100_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, 1000, 101, 70, 900, 45);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPercentage");
    }

    [Fact]
    public void Create_WithNegativeQ30Percentage_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, 1000, 80, -1, 900, 45);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPercentage");
    }

    [Fact]
    public void Create_WithQ30PercentageOver100_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, 1000, 80, 101, 900, 45);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPercentage");
    }

    [Fact]
    public void Create_WithNegativeTrimmedLength_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, 1000, 80, 70, -1, 45);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimmedLength");
    }

    [Fact]
    public void Create_WithTrimmedLengthGreaterThanTotal_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, 1000, 80, 70, 1001, 45);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimmedLength");
    }

    [Fact]
    public void Create_WithNegativeGcContent_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, 1000, 80, 70, 900, -1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPercentage");
    }

    [Fact]
    public void Create_WithGcContentOver100_ShouldFail()
    {
        // Act
        var result = QualityMetrics.Create(30, 1000, 80, 70, 900, 101);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidPercentage");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldReturnMetricsWithAllZeros()
    {
        // Act
        var empty = QualityMetrics.Empty;

        // Assert
        empty.AverageQualityScore.Should().Be(0);
        empty.TotalBases.Should().Be(0);
        empty.QualityAboveQ20Percentage.Should().Be(0);
        empty.QualityAboveQ30Percentage.Should().Be(0);
        empty.TrimmedLength.Should().Be(0);
        empty.GcContentPercentage.Should().Be(0);
    }

    #endregion

    #region Quality Indicators

    [Fact]
    public void IsGoodQuality_WithScoreAbove20_ShouldReturnTrue()
    {
        // Arrange
        var metrics = QualityMetrics.Create(25, 1000, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics.IsGoodQuality.Should().BeTrue();
    }

    [Fact]
    public void IsGoodQuality_WithScoreExactly20_ShouldReturnTrue()
    {
        // Arrange
        var metrics = QualityMetrics.Create(20, 1000, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics.IsGoodQuality.Should().BeTrue();
    }

    [Fact]
    public void IsGoodQuality_WithScoreBelow20_ShouldReturnFalse()
    {
        // Arrange
        var metrics = QualityMetrics.Create(19, 1000, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics.IsGoodQuality.Should().BeFalse();
    }

    [Fact]
    public void IsHighQuality_WithScoreAbove30_ShouldReturnTrue()
    {
        // Arrange
        var metrics = QualityMetrics.Create(35, 1000, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics.IsHighQuality.Should().BeTrue();
    }

    [Fact]
    public void IsHighQuality_WithScoreExactly30_ShouldReturnTrue()
    {
        // Arrange
        var metrics = QualityMetrics.Create(30, 1000, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics.IsHighQuality.Should().BeTrue();
    }

    [Fact]
    public void IsHighQuality_WithScoreBelow30_ShouldReturnFalse()
    {
        // Arrange
        var metrics = QualityMetrics.Create(29, 1000, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics.IsHighQuality.Should().BeFalse();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var metrics1 = QualityMetrics.Create(30, 1000, 80, 70, 900, 45).Value;
        var metrics2 = QualityMetrics.Create(30, 1000, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics1.Should().Be(metrics2);
    }

    [Fact]
    public void Equals_WithDifferentQualityScoREDACTED()
    {
        // Arrange
        var metrics1 = QualityMetrics.Create(30, 1000, 80, 70, 900, 45).Value;
        var metrics2 = QualityMetrics.Create(31, 1000, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics1.Should().NotBe(metrics2);
    }

    [Fact]
    public void Equals_WithDifferentTotalBases_ShouldNotBeEqual()
    {
        // Arrange
        var metrics1 = QualityMetrics.Create(30, 1000, 80, 70, 900, 45).Value;
        var metrics2 = QualityMetrics.Create(30, 1001, 80, 70, 900, 45).Value;

        // Act & Assert
        metrics1.Should().NotBe(metrics2);
    }

    #endregion
}
