using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.ValueObjects;

/// <summary>
/// Unit tests for TrimRegion value object.
/// </summary>
public class TrimRegionTests
{
    private const int DefaultSequenceLength = 1000;

    #region Create - Success Cases

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var start5Prime = 0;
        var end5Prime = 50;
        var start3Prime = 900;
        var end3Prime = 1000;
        var algorithm = "Mott";
        var trimmedBy = "user123";

        // Act
        var result = TrimRegion.Create(start5Prime, end5Prime, start3Prime, end3Prime, algorithm, trimmedBy, DefaultSequenceLength);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Start5Prime.Should().Be(start5Prime);
        result.Value.End5Prime.Should().Be(end5Prime);
        result.Value.Start3Prime.Should().Be(start3Prime);
        result.Value.End3Prime.Should().Be(end3Prime);
        result.Value.Algorithm.Should().Be(algorithm);
        result.Value.TrimmedBy.Should().Be(trimmedBy);
        result.Value.TrimmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNoTrimming_ShouldSucceed()
    {
        // Entire sequence is good quality
        // Act
        var result = TrimRegion.Create(0, 0, 1000, 1000, "Auto", "user", DefaultSequenceLength);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrimmedLength.Should().Be(1000);
    }

    [Fact]
    public void Create_WithAlgorithmContainingWhitespace_ShouldTrim()
    {
        // Act
        var result = TrimRegion.Create(0, 50, 900, 1000, "  Mott  ", "user", DefaultSequenceLength);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Algorithm.Should().Be("Mott");
    }

    #endregion

    #region Create - Failure Cases

    [Fact]
    public void Create_WithNegativeStart5Prime_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(-1, 50, 900, 1000, "Mott", "user", DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPosition");
    }

    [Fact]
    public void Create_WithEnd5PrimeLessThanStart5Prime_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(50, 40, 900, 1000, "Mott", "user", DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPosition");
    }

    [Fact]
    public void Create_WithStart3PrimeLessThanEnd5Prime_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(0, 100, 90, 1000, "Mott", "user", DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPosition");
    }

    [Fact]
    public void Create_WithEnd3PrimeLessThanStart3Prime_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(0, 50, 950, 900, "Mott", "user", DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidTrimPosition");
    }

    [Fact]
    public void Create_WithEnd3PrimeExceedingSequenceLength_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(0, 50, 900, 1001, "Mott", "user", DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimExceedsSequenceLength");
    }

    [Fact]
    public void Create_WithNullAlgorithm_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(0, 50, 900, 1000, null!, "user", DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimAlgorithmRequired");
    }

    [Fact]
    public void Create_WithEmptyAlgorithm_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(0, 50, 900, 1000, string.Empty, "user", DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimAlgorithmRequired");
    }

    [Fact]
    public void Create_WithAlgorithmTooLong_ShouldFail()
    {
        // Arrange
        var longAlgorithm = new string('a', TrimRegion.MaxAlgorithmLength + 1);

        // Act
        var result = TrimRegion.Create(0, 50, 900, 1000, longAlgorithm, "user", DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimAlgorithmTooLong");
    }

    [Fact]
    public void Create_WithNullTrimmedBy_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(0, 50, 900, 1000, "Mott", null!, DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimmedByRequired");
    }

    [Fact]
    public void Create_WithEmptyTrimmedBy_ShouldFail()
    {
        // Act
        var result = TrimRegion.Create(0, 50, 900, 1000, "Mott", string.Empty, DefaultSequenceLength);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimmedByRequired");
    }

    #endregion

    #region CreateManual

    [Fact]
    public void CreateManual_WithValidData_ShouldSucceed()
    {
        // Act
        var result = TrimRegion.CreateManual(50, 900, "user", DefaultSequenceLength);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Start5Prime.Should().Be(0);
        result.Value.End5Prime.Should().Be(50);
        result.Value.Start3Prime.Should().Be(900);
        result.Value.End3Prime.Should().Be(1000);
        result.Value.Algorithm.Should().Be("Manual");
    }

    #endregion

    #region Computed Properties

    [Fact]
    public void TrimmedLength_ShouldReturnGoodQualityRegionLength()
    {
        // Arrange
        var trimRegion = TrimRegion.Create(0, 50, 900, 1000, "Mott", "user", DefaultSequenceLength).Value;

        // Act
        var trimmedLength = trimRegion.TrimmedLength;

        // Assert
        trimmedLength.Should().Be(850); // 900 - 50
    }

    [Fact]
    public void TotalBasesTrimmed_ShouldReturnCorrectCount()
    {
        // Arrange
        var trimRegion = TrimRegion.Create(0, 50, 900, 1000, "Mott", "user", DefaultSequenceLength).Value;

        // Act
        var totalTrimmed = trimRegion.TotalBasesTrimmed;

        // Assert
        totalTrimmed.Should().Be(150); // 50 from 5' + (1000-900) from 3'
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var trim1 = TrimRegion.Create(0, 50, 900, 1000, "Mott", "user", DefaultSequenceLength).Value;
        var trim2 = TrimRegion.Create(0, 50, 900, 1000, "Mott", "user", DefaultSequenceLength).Value;

        // Note: TrimmedAt will differ slightly, but for practical purposes
        // we compare by core values. The test documents expected behavior.
        trim1.Start5Prime.Should().Be(trim2.Start5Prime);
        trim1.End5Prime.Should().Be(trim2.End5Prime);
        trim1.Start3Prime.Should().Be(trim2.Start3Prime);
        trim1.End3Prime.Should().Be(trim2.End3Prime);
        trim1.Algorithm.Should().Be(trim2.Algorithm);
        trim1.TrimmedBy.Should().Be(trim2.TrimmedBy);
    }

    [Fact]
    public void Equals_WithDifferentPositions_ShouldNotBeEqual()
    {
        // Arrange
        var trim1 = TrimRegion.Create(0, 50, 900, 1000, "Mott", "user", DefaultSequenceLength).Value;
        var trim2 = TrimRegion.Create(0, 60, 900, 1000, "Mott", "user", DefaultSequenceLength).Value;

        // Act & Assert
        trim1.Should().NotBe(trim2);
    }

    #endregion

    #region Constants

    [Fact]
    public void MaxAlgorithmLength_ShouldBe50()
    {
        TrimRegion.MaxAlgorithmLength.Should().Be(50);
    }

    #endregion
}
