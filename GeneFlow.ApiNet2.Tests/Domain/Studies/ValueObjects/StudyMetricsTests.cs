using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.ValueObjects;

/// <summary>
/// Unit tests for the StudyMetrics value object.
/// </summary>
public class StudyMetricsTests
{
    #region Empty

    [Fact]
    public void Empty_ShouldHaveZeroCounts()
    {
        // Act
        var metrics = StudyMetrics.Empty;

        // Assert
        metrics.ViewsCount.Should().Be(0);
        metrics.StarsCount.Should().Be(0);
    }

    #endregion

    #region Create

    [Fact]
    public void Create_WithValidValues_ShouldReturnCorrectMetrics()
    {
        // Act
        var metrics = StudyMetrics.Create(100, 25);

        // Assert
        metrics.ViewsCount.Should().Be(100);
        metrics.StarsCount.Should().Be(25);
    }

    [Fact]
    public void Create_WithZeros_ShouldReturnZeroMetrics()
    {
        // Act
        var metrics = StudyMetrics.Create(0, 0);

        // Assert
        metrics.ViewsCount.Should().Be(0);
        metrics.StarsCount.Should().Be(0);
    }

    #endregion

    #region IncrementViews

    [Fact]
    public void IncrementViews_ShouldIncreaseViewsCountByOne()
    {
        // Arrange
        var metrics = StudyMetrics.Create(10, 5);

        // Act
        var updated = metrics.IncrementViews();

        // Assert
        updated.ViewsCount.Should().Be(11);
        updated.StarsCount.Should().Be(5);
    }

    [Fact]
    public void IncrementViews_FromEmpty_ShouldReturnOne()
    {
        // Arrange
        var metrics = StudyMetrics.Empty;

        // Act
        var updated = metrics.IncrementViews();

        // Assert
        updated.ViewsCount.Should().Be(1);
        updated.StarsCount.Should().Be(0);
    }

    [Fact]
    public void IncrementViews_ShouldNotMutateOriginal()
    {
        // Arrange
        var original = StudyMetrics.Create(10, 5);

        // Act
        var updated = original.IncrementViews();

        // Assert
        original.ViewsCount.Should().Be(10);
        updated.ViewsCount.Should().Be(11);
    }

    #endregion

    #region IncrementStars

    [Fact]
    public void IncrementStars_ShouldIncreaseStarsCountByOne()
    {
        // Arrange
        var metrics = StudyMetrics.Create(10, 5);

        // Act
        var updated = metrics.IncrementStars();

        // Assert
        updated.StarsCount.Should().Be(6);
        updated.ViewsCount.Should().Be(10);
    }

    [Fact]
    public void IncrementStars_FromEmpty_ShouldReturnOne()
    {
        // Arrange
        var metrics = StudyMetrics.Empty;

        // Act
        var updated = metrics.IncrementStars();

        // Assert
        updated.StarsCount.Should().Be(1);
        updated.ViewsCount.Should().Be(0);
    }

    [Fact]
    public void IncrementStars_ShouldNotMutateOriginal()
    {
        // Arrange
        var original = StudyMetrics.Create(10, 5);

        // Act
        var updated = original.IncrementStars();

        // Assert
        original.StarsCount.Should().Be(5);
        updated.StarsCount.Should().Be(6);
    }

    #endregion

    #region DecrementStars

    [Fact]
    public void DecrementStars_ShouldDecreaseStarsCountByOne()
    {
        // Arrange
        var metrics = StudyMetrics.Create(10, 5);

        // Act
        var updated = metrics.DecrementStars();

        // Assert
        updated.StarsCount.Should().Be(4);
        updated.ViewsCount.Should().Be(10);
    }

    [Fact]
    public void DecrementStars_AtZero_ShouldRemainZero()
    {
        // Arrange
        var metrics = StudyMetrics.Empty;

        // Act
        var updated = metrics.DecrementStars();

        // Assert
        updated.StarsCount.Should().Be(0);
    }

    [Fact]
    public void DecrementStars_ShouldNotMutateOriginal()
    {
        // Arrange
        var original = StudyMetrics.Create(10, 5);

        // Act
        var updated = original.DecrementStars();

        // Assert
        original.StarsCount.Should().Be(5);
        updated.StarsCount.Should().Be(4);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var metrics1 = StudyMetrics.Create(100, 25);
        var metrics2 = StudyMetrics.Create(100, 25);

        // Assert
        metrics1.Should().Be(metrics2);
    }

    [Fact]
    public void Equals_WithDifferentViews_ShouldReturnFalse()
    {
        // Arrange
        var metrics1 = StudyMetrics.Create(100, 25);
        var metrics2 = StudyMetrics.Create(101, 25);

        // Assert
        metrics1.Should().NotBe(metrics2);
    }

    [Fact]
    public void Equals_WithDifferentStars_ShouldReturnFalse()
    {
        // Arrange
        var metrics1 = StudyMetrics.Create(100, 25);
        var metrics2 = StudyMetrics.Create(100, 26);

        // Assert
        metrics1.Should().NotBe(metrics2);
    }

    [Fact]
    public void Equals_BothEmpty_ShouldBeEqual()
    {
        // Arrange
        var metrics1 = StudyMetrics.Empty;
        var metrics2 = StudyMetrics.Create(0, 0);

        // Assert
        metrics1.Should().Be(metrics2);
    }

    #endregion
}
