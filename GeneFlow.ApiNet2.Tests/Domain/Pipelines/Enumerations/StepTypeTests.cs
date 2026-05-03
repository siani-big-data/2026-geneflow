using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Pipelines.Enumerations;

/// <summary>
/// Unit tests for StepType smart enumeration.
/// </summary>
public class StepTypeTests
{
    #region Static Values

    [Fact]
    public void Quality_ShouldHaveCorrectProperties()
    {
        // Assert
        StepType.Quality.Id.Should().Be(1);
        StepType.Quality.Name.Should().Be("Quality");
        StepType.Quality.DisplayName.Should().Be("Quality Analysis");
        StepType.Quality.AnalysisKey.Should().Be("quality");
        StepType.Quality.RequiresConfiguration.Should().BeFalse();
    }

    [Fact]
    public void Trimming_ShouldHaveCorrectProperties()
    {
        // Assert
        StepType.Trimming.Id.Should().Be(2);
        StepType.Trimming.Name.Should().Be("Trimming");
        StepType.Trimming.DisplayName.Should().Be("Sequence Trimming");
        StepType.Trimming.AnalysisKey.Should().Be("trimming");
        StepType.Trimming.RequiresConfiguration.Should().BeTrue();
    }

    [Fact]
    public void Heterozygote_ShouldHaveCorrectProperties()
    {
        // Assert
        StepType.Heterozygote.Id.Should().Be(3);
        StepType.Heterozygote.Name.Should().Be("Heterozygote");
        StepType.Heterozygote.DisplayName.Should().Be("Heterozygote Detection");
        StepType.Heterozygote.AnalysisKey.Should().Be("heterozygote");
        StepType.Heterozygote.RequiresConfiguration.Should().BeTrue();
    }

    [Fact]
    public void Motif_ShouldHaveCorrectProperties()
    {
        // Assert
        StepType.Motif.Id.Should().Be(4);
        StepType.Motif.Name.Should().Be("Motif");
        StepType.Motif.DisplayName.Should().Be("Motif Search");
        StepType.Motif.AnalysisKey.Should().Be("motif");
        StepType.Motif.RequiresConfiguration.Should().BeTrue();
    }

    [Fact]
    public void Translation_ShouldHaveCorrectProperties()
    {
        // Assert
        StepType.Translation.Id.Should().Be(5);
        StepType.Translation.Name.Should().Be("Translation");
        StepType.Translation.DisplayName.Should().Be("Sequence Translation");
        StepType.Translation.AnalysisKey.Should().Be("translation");
        StepType.Translation.RequiresConfiguration.Should().BeTrue();
    }

    [Fact]
    public void ORF_ShouldHaveCorrectProperties()
    {
        // Assert
        StepType.ORF.Id.Should().Be(6);
        StepType.ORF.Name.Should().Be("ORF");
        StepType.ORF.DisplayName.Should().Be("Open Reading Frame Detection");
        StepType.ORF.AnalysisKey.Should().Be("orf");
        StepType.ORF.RequiresConfiguration.Should().BeTrue();
    }

    [Fact]
    public void Restriction_ShouldHaveCorrectProperties()
    {
        // Assert
        StepType.Restriction.Id.Should().Be(7);
        StepType.Restriction.Name.Should().Be("Restriction");
        StepType.Restriction.DisplayName.Should().Be("Restriction Site Analysis");
        StepType.Restriction.AnalysisKey.Should().Be("restriction");
        StepType.Restriction.RequiresConfiguration.Should().BeTrue();
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllStepTypes()
    {
        // Act
        var allTypes = StepType.GetAll();

        // Assert
        allTypes.Should().HaveCount(7);
        allTypes.Should().Contain(StepType.Quality);
        allTypes.Should().Contain(StepType.Trimming);
        allTypes.Should().Contain(StepType.Heterozygote);
        allTypes.Should().Contain(StepType.Motif);
        allTypes.Should().Contain(StepType.Translation);
        allTypes.Should().Contain(StepType.ORF);
        allTypes.Should().Contain(StepType.Restriction);
    }

    [Fact]
    public void All_ShouldHaveUniqueIds()
    {
        // Act
        var allTypes = StepType.GetAll();
        var ids = allTypes.Select(t => t.Id);

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ShouldHaveUniqueNames()
    {
        // Act
        var allTypes = StepType.GetAll();
        var names = allTypes.Select(t => t.Name);

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_ShouldHaveUniqueAnalysisKeys()
    {
        // Act
        var allTypes = StepType.GetAll();
        var keys = allTypes.Select(t => t.AnalysisKey);

        // Assert
        keys.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Quality")]
    [InlineData(2, "Trimming")]
    [InlineData(3, "Heterozygote")]
    [InlineData(4, "Motif")]
    [InlineData(5, "Translation")]
    [InlineData(6, "ORF")]
    [InlineData(7, "Restriction")]
    public void FromId_WithValidId_ShouldReturnCorrectType(int id, string expectedName)
    {
        // Act
        var stepType = StepType.FromId(id);

        // Assert
        stepType.Should().NotBeNull();
        stepType!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var stepType = StepType.FromId(999);

        // Assert
        stepType.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Quality", 1)]
    [InlineData("Trimming", 2)]
    [InlineData("Heterozygote", 3)]
    [InlineData("Motif", 4)]
    [InlineData("Translation", 5)]
    [InlineData("ORF", 6)]
    [InlineData("Restriction", 7)]
    public void FromName_WithValidName_ShouldReturnCorrectType(string name, int expectedId)
    {
        // Act
        var stepType = StepType.FromName(name);

        // Assert
        stepType.Should().NotBeNull();
        stepType!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var stepType = StepType.FromName("InvalidType");

        // Assert
        stepType.Should().BeNull();
    }

    #endregion

    #region RequiresConfiguration

    [Fact]
    public void RequiresConfiguration_Quality_ShouldReturnFalse()
    {
        // Assert - Quality is the only step that doesn't require configuration
        StepType.Quality.RequiresConfiguration.Should().BeFalse();
    }

    [Theory]
    [InlineData("Trimming")]
    [InlineData("Heterozygote")]
    [InlineData("Motif")]
    [InlineData("Translation")]
    [InlineData("ORF")]
    [InlineData("Restriction")]
    public void RequiresConfiguration_OtherTypes_ShouldReturnTrue(string typeName)
    {
        // Arrange
        var stepType = StepType.FromName(typeName)!;

        // Assert
        stepType.RequiresConfiguration.Should().BeTrue();
    }

    #endregion

    #region AnalysisKey

    [Fact]
    public void AnalysisKey_ShouldBeLowerCase()
    {
        // Assert
        foreach (var stepType in StepType.GetAll())
        {
            stepType.AnalysisKey.Should().Be(stepType.AnalysisKey.ToLowerInvariant());
        }
    }

    [Fact]
    public void AnalysisKey_ShouldNotBeEmpty()
    {
        // Assert
        foreach (var stepType in StepType.GetAll())
        {
            stepType.AnalysisKey.Should().NotBeNullOrWhiteSpace();
        }
    }

    #endregion

    #region DisplayName

    [Fact]
    public void DisplayName_ShouldBeHumanReadable()
    {
        // Assert
        foreach (var stepType in StepType.GetAll())
        {
            stepType.DisplayName.Should().NotBeNullOrWhiteSpace();
            stepType.DisplayName.Should().Contain(" "); // Multi-word display names
        }
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameType_ShouldReturnTrue()
    {
        // Arrange
        var type1 = StepType.Quality;
        var type2 = StepType.FromId(1);

        // Assert
        (type1 == type2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var type1 = StepType.Quality;
        var type2 = StepType.Trimming;

        // Assert
        (type1 != type2).Should().BeTrue();
    }

    #endregion
}
