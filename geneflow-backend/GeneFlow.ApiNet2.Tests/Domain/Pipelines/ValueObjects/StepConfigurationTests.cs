using System.Globalization;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.ValueObjects;

/// <summary>
/// Unit tests for the StepConfiguration value object.
/// </summary>
public class StepConfigurationTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidJson_ShouldReturnSuccess()
    {
        // Arrange
        const string json = """{"threshold": 0.5, "window": 10}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Quality);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(json);
    }

    [Fact]
    public void Create_WithNullForNonRequiredStep_ShouldReturnEmptyConfiguration()
    {
        // Act - Quality step does not require configuration
        var result = StepConfiguration.Create(null, StepType.Quality);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("{}");
    }

    [Fact]
    public void Create_WithEmptyJsonObject_ShouldReturnSuccessForNonRequiredStep()
    {
        // Act
        var result = StepConfiguration.Create("{}", StepType.Quality);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        const string json = """  {"key": "value"}  """;

        // Act
        var result = StepConfiguration.Create(json, StepType.Quality);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("""{"key": "value"}""");
    }

    #endregion

    #region Create - Invalid Cases

    [Fact]
    public void Create_WithInvalidJson_ShouldReturnFailure()
    {
        // Arrange
        const string invalidJson = "{ invalid json }";

        // Act
        var result = StepConfiguration.Create(invalidJson, StepType.Quality);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidStepConfiguration");
    }

    [Fact]
    public void Create_WithJsonTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longValue = new string('a', StepConfiguration.MaxLength);
        var longJson = $$"""{ "key": "{{longValue}}" }""";

        // Act
        var result = StepConfiguration.Create(longJson, StepType.Quality);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StepConfigurationTooLong");
    }

    [Fact]
    public void Create_WithNullForRequiredStep_ShouldReturnFailure()
    {
        // Act - Motif step requires configuration
        var result = StepConfiguration.Create(null, StepType.Motif);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("StepConfigurationRequired");
    }

    #endregion

    #region Step Type Specific Validation - Quality Trimming

    [Fact]
    public void Create_QualityTrimming_WithValidCutoff_ShouldReturnSuccess()
    {
        // Arrange
        const string json = """{"cutoff": 0.1}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Trimming);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    public void Create_QualityTrimming_WithCutoffInRange_ShouldReturnSuccess(double cutoff)
    {
        // Arrange - use InvariantCulture to ensure correct decimal separator
        var cutoffStr = cutoff.ToString(CultureInfo.InvariantCulture);
        var json = $$"""{"cutoff": {{cutoffStr}}}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Trimming);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.0)]
    [InlineData(0.6)]
    [InlineData(1.0)]
    public void Create_QualityTrimming_WithCutoffOutOfRange_ShouldReturnFailure(double cutoff)
    {
        // Arrange - use InvariantCulture to ensure correct decimal separator
        var cutoffStr = cutoff.ToString(CultureInfo.InvariantCulture);
        var json = $$"""{"cutoff": {{cutoffStr}}}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Trimming);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("TrimmingCutoffOutOfRange");
    }

    #endregion

    #region Step Type Specific Validation - Motif Search

    [Fact]
    public void Create_MotifSearch_WithPattern_ShouldReturnSuccess()
    {
        // Arrange
        const string json = """{"pattern": "ATCG"}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Motif);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_MotifSearch_WithoutPattern_ShouldReturnFailure()
    {
        // Arrange
        const string json = """{"other": "value"}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Motif);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("MotifPatternRequired");
    }

    [Fact]
    public void Create_MotifSearch_WithEmptyPattern_ShouldReturnFailure()
    {
        // Arrange
        const string json = """{"pattern": ""}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Motif);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("MotifPatternRequired");
    }

    [Fact]
    public void Create_MotifSearch_WithWhitespacePattern_ShouldReturnFailure()
    {
        // Arrange
        const string json = """{"pattern": "   "}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Motif);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("MotifPatternRequired");
    }

    #endregion

    #region Step Type Specific Validation - Restriction Enzyme

    [Fact]
    public void Create_RestrictionEnzyme_WithEnzymes_ShouldReturnSuccess()
    {
        // Arrange
        const string json = """{"enzymes": ["EcoRI", "BamHI"]}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Restriction);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_RestrictionEnzyme_WithoutEnzymes_ShouldReturnFailure()
    {
        // Arrange
        const string json = """{"other": "value"}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Restriction);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("RestrictionEnzymesRequired");
    }

    [Fact]
    public void Create_RestrictionEnzyme_WithEmptyEnzymesList_ShouldReturnFailure()
    {
        // Arrange
        const string json = """{"enzymes": []}""";

        // Act
        var result = StepConfiguration.Create(json, StepType.Restriction);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("RestrictionEnzymesRequired");
    }

    #endregion

    #region GetValue

    [Fact]
    public void GetValue_ExistingKey_ShouldReturnValue()
    {
        // Arrange
        const string json = """{"threshold": 0.5}""";
        var config = StepConfiguration.Create(json, StepType.Quality).Value;

        // Act
        var result = config.GetValue<double>("threshold");

        // Assert
        result.Should().Be(0.5);
    }

    [Fact]
    public void GetValue_NonExistingKey_ShouldReturnDefault()
    {
        // Arrange
        const string json = """{"threshold": 0.5}""";
        var config = StepConfiguration.Create(json, StepType.Quality).Value;

        // Act
        var result = config.GetValue<double>("nonexistent", 1.0);

        // Assert
        result.Should().Be(1.0);
    }

    [Fact]
    public void GetValue_StringKey_ShouldReturnString()
    {
        // Arrange
        const string json = """{"pattern": "ATCG"}""";
        var config = StepConfiguration.Create(json, StepType.Motif).Value;

        // Act
        var result = config.GetValue<string>("pattern");

        // Assert
        result.Should().Be("ATCG");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldHaveEmptyJsonObject()
    {
        // Act
        var empty = StepConfiguration.Empty;

        // Assert
        empty.Value.Should().Be("{}");
    }

    #endregion

    #region ToDictionary

    [Fact]
    public void ToDictionary_WithValues_ShouldReturnDictionary()
    {
        // Arrange
        const string json = """{"key1": "value1", "key2": 42}""";
        var config = StepConfiguration.Create(json, StepType.Quality).Value;

        // Act
        var dict = config.ToDictionary();

        // Assert
        dict.Should().ContainKey("key1");
        dict.Should().ContainKey("key2");
    }

    [Fact]
    public void ToDictionary_WhenEmpty_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var config = StepConfiguration.Empty;

        // Act
        var dict = config.ToDictionary();

        // Assert
        dict.Should().BeEmpty();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameJson_ShouldBeEqual()
    {
        // Arrange
        var config1 = StepConfiguration.Create("""{"key": "value"}""", StepType.Quality).Value;
        var config2 = StepConfiguration.Create("""{"key": "value"}""", StepType.Quality).Value;

        // Assert
        config1.Should().Be(config2);
    }

    [Fact]
    public void Equals_DifferentJson_ShouldNotBeEqual()
    {
        // Arrange
        var config1 = StepConfiguration.Create("""{"key": "value1"}""", StepType.Quality).Value;
        var config2 = StepConfiguration.Create("""{"key": "value2"}""", StepType.Quality).Value;

        // Assert
        config1.Should().NotBe(config2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnJsonValue()
    {
        // Arrange
        const string json = """{"key": "value"}""";
        var config = StepConfiguration.Create(json, StepType.Quality).Value;

        // Act
        var result = config.ToString();

        // Assert
        result.Should().Be(json);
    }

    #endregion
}
