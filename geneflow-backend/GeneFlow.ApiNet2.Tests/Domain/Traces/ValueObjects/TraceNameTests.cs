using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.ValueObjects;

/// <summary>
/// Unit tests for TraceName value object.
/// </summary>
public class TraceNameTests
{
    #region Create - Success Cases

    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        // Arrange
        var name = "Sample_001.ab1";

        // Act
        var result = TraceName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(name);
    }

    [Fact]
    public void Create_WithMinimumLength_ShouldSucceed()
    {
        // Arrange
        var name = new string('A', TraceName.MinLength);

        // Act
        var result = TraceName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(name);
    }

    [Fact]
    public void Create_WithMaximumLength_ShouldSucceed()
    {
        // Arrange
        var name = new string('A', TraceName.MaxLength);

        // Act
        var result = TraceName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(name);
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrimAndSucceed()
    {
        // Arrange
        var name = "  Sample_001.ab1  ";

        // Act
        var result = TraceName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Sample_001.ab1");
    }

    [Theory]
    [InlineData("BRCA1_forward")]
    [InlineData("sample-123")]
    [InlineData("trace_2024_01_15")]
    [InlineData("seq.ab1")]
    public void Create_WithVariousValidNames_ShouldSucceed(string name)
    {
        // Act
        var result = TraceName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(name);
    }

    #endregion

    #region Create - Failure Cases

    [Fact]
    public void Create_WithNull_ShouldFail()
    {
        // Act
        var result = TraceName.Create(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameRequired");
    }

    [Fact]
    public void Create_WithEmptyString_ShouldFail()
    {
        // Act
        var result = TraceName.Create(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameRequired");
    }

    [Fact]
    public void Create_WithOnlyWhitespace_ShouldFail()
    {
        // Act
        var result = TraceName.Create("   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameRequired");
    }

    [Fact]
    public void Create_WithNameTooShort_ShouldFail()
    {
        // Arrange
        var name = new string('A', TraceName.MinLength - 1);

        // Act
        var result = TraceName.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameTooShort");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldFail()
    {
        // Arrange
        var name = new string('A', TraceName.MaxLength + 1);

        // Act
        var result = TraceName.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NameTooLong");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameName_ShouldBeEqual()
    {
        // Arrange
        var name1 = TraceName.Create("Sample_001").Value;
        var name2 = TraceName.Create("Sample_001").Value;

        // Act & Assert
        name1.Should().Be(name2);
    }

    [Fact]
    public void Equals_WithDifferentNames_ShouldNotBeEqual()
    {
        // Arrange
        var name1 = TraceName.Create("Sample_001").Value;
        var name2 = TraceName.Create("Sample_002").Value;

        // Act & Assert
        name1.Should().NotBe(name2);
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var traceName = TraceName.Create("Sample_001").Value;

        // Act
        string result = traceName;

        // Assert
        result.Should().Be("Sample_001");
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var traceName = TraceName.Create("Sample_001").Value;

        // Act
        var result = traceName.ToString();

        // Assert
        result.Should().Be("Sample_001");
    }

    #endregion

    #region Constants

    [Fact]
    public void MinLength_ShouldBeThree()
    {
        TraceName.MinLength.Should().Be(3);
    }

    [Fact]
    public void MaxLength_ShouldBeOneHundred()
    {
        TraceName.MaxLength.Should().Be(100);
    }

    #endregion
}
