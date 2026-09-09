using GeneFlow.ApiNet2.Domain.Traces;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces;

/// <summary>
/// Unit tests for TraceId strongly-typed identifier.
/// </summary>
public class TraceIdTests
{
    #region New

    [Fact]
    public void New_ShouldCreateTraceId_WithNewGuid()
    {
        // Act
        var traceId = TraceId.New();

        // Assert
        traceId.Should().NotBeNull();
        traceId.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void New_ShouldCreateUniqueIds_WhenCalledMultipleTimes()
    {
        // Act
        var id1 = TraceId.New();
        var id2 = TraceId.New();
        var id3 = TraceId.New();

        // Assert
        id1.Should().NotBe(id2);
        id2.Should().NotBe(id3);
        id1.Should().NotBe(id3);
    }

    #endregion

    #region From

    [Fact]
    public void From_ShouldCreateTraceId_WithSpecifiedGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var traceId = TraceId.From(guid);

        // Assert
        traceId.Value.Should().Be(guid);
    }

    [Fact]
    public void From_ShouldCreateEquivalentIds_WhenSameGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id1 = TraceId.From(guid);
        var id2 = TraceId.From(guid);

        // Assert
        id1.Should().Be(id2);
    }

    #endregion

    #region Parse

    [Fact]
    public void Parse_ShouldCreateTraceId_FromValidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var traceId = TraceId.Parse(guidString);

        // Assert
        traceId.Value.Should().Be(guid);
    }

    [Fact]
    public void Parse_ShouldThrowException_WithInvalidString()
    {
        // Arrange
        var invalidString = "not-a-guid";

        // Act
        var act = () => TraceId.Parse(invalidString);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_ShouldThrowException_WithEmptyString()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        var act = () => TraceId.Parse(emptyString);

        // Assert
        act.Should().Throw<FormatException>();
    }

    #endregion

    #region TryParse

    [Fact]
    public void TryParse_ShouldReturnTrue_WithValidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var success = TraceId.TryParse(guidString, out var traceId);

        // Assert
        success.Should().BeTrue();
        traceId.Should().NotBeNull();
        traceId!.Value.Should().Be(guid);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WithInvalidString()
    {
        // Arrange
        var invalidString = "not-a-guid";

        // Act
        var success = TraceId.TryParse(invalidString, out var traceId);

        // Assert
        success.Should().BeFalse();
        traceId.Should().BeNull();
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WithNullString()
    {
        // Act
        var success = TraceId.TryParse(null, out var traceId);

        // Assert
        success.Should().BeFalse();
        traceId.Should().BeNull();
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WithEmptyString()
    {
        // Act
        var success = TraceId.TryParse(string.Empty, out var traceId);

        // Assert
        success.Should().BeFalse();
        traceId.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSameGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = TraceId.From(guid);
        var id2 = TraceId.From(guid);

        // Act & Assert
        id1.Equals(id2).Should().BeTrue();
        (id1 == id2).Should().BeTrue();
        (id1 != id2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDifferentGuids()
    {
        // Arrange
        var id1 = TraceId.New();
        var id2 = TraceId.New();

        // Act & Assert
        id1.Equals(id2).Should().BeFalse();
        (id1 == id2).Should().BeFalse();
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedToNull()
    {
        // Arrange
        var traceId = TraceId.New();

        // Act & Assert
        traceId.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHash_ForEqualIds()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = TraceId.From(guid);
        var id2 = TraceId.From(guid);

        // Act & Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    #endregion

    #region Implicit Conversions

    [Fact]
    public void ImplicitConversion_ToGuid_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var traceId = TraceId.From(guid);

        // Act
        Guid result = traceId;

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var traceId = TraceId.From(guid);

        // Act
        string result = traceId;

        // Assert
        result.Should().Be(guid.ToString());
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var traceId = TraceId.From(guid);

        // Act
        var result = traceId.ToString();

        // Assert
        result.Should().Be(guid.ToString());
    }

    #endregion
}
