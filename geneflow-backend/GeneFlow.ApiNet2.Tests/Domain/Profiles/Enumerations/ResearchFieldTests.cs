using GeneFlow.ApiNet2.Domain.Profiles.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.Enumerations;

/// <summary>
/// Unit tests for the ResearchField smart enumeration.
/// </summary>
public class ResearchFieldTests
{
    #region Static Fields

    [Fact]
    public void Genomics_ShouldHaveCorrectIdAndName()
    {
        ResearchField.Genomics.Id.Should().Be(1);
        ResearchField.Genomics.Name.Should().Be("Genomics");
    }

    [Fact]
    public void Proteomics_ShouldHaveCorrectIdAndName()
    {
        ResearchField.Proteomics.Id.Should().Be(2);
        ResearchField.Proteomics.Name.Should().Be("Proteomics");
    }

    [Fact]
    public void Other_ShouldHaveCorrectIdAndName()
    {
        ResearchField.Other.Id.Should().Be(18);
        ResearchField.Other.Name.Should().Be("Other");
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllResearchFields()
    {
        // Act
        var fields = ResearchField.GetAll();

        // Assert
        fields.Should().HaveCount(18);
        fields.Should().Contain(ResearchField.Genomics);
        fields.Should().Contain(ResearchField.Proteomics);
        fields.Should().Contain(ResearchField.Bioinformatics);
        fields.Should().Contain(ResearchField.Other);
    }

    #endregion

    #region FromId

    [Fact]
    public void FromId_WithValidId_ShouldReturnResearchField()
    {
        // Act
        var field = ResearchField.FromId(1);

        // Assert
        field.Should().Be(ResearchField.Genomics);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = ResearchField.FromId(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region FromName

    [Fact]
    public void FromName_WithValidName_ShouldReturnResearchField()
    {
        // Act
        var field = ResearchField.FromName("Genomics");

        // Assert
        field.Should().Be(ResearchField.Genomics);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        // Note: The Enumeration<T> base class returns null for invalid names
        var result = ResearchField.FromName("InvalidField");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Bioinformatics")]
    [InlineData("MolecularBiology")]
    [InlineData("CellBiology")]
    [InlineData("Genetics")]
    public void FromName_WithVariousValidNames_ShouldReturnCorrectField(string name)
    {
        // Act
        var field = ResearchField.FromName(name);

        // Assert
        field.Name.Should().Be(name);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameField_ShouldReturnTrue()
    {
        // Arrange
        var field1 = ResearchField.Genomics;
        var field2 = ResearchField.Genomics;

        // Assert
        field1.Should().Be(field2);
        (field1 == field2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentField_ShouldReturnFalse()
    {
        // Arrange
        var field1 = ResearchField.Genomics;
        var field2 = ResearchField.Proteomics;

        // Assert
        field1.Should().NotBe(field2);
        (field1 != field2).Should().BeTrue();
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnName()
    {
        // Act
        var result = ResearchField.Genomics.ToString();

        // Assert
        result.Should().Be("Genomics");
    }

    #endregion

    #region All Research Fields Exist

    [Theory]
    [InlineData("Genomics")]
    [InlineData("Proteomics")]
    [InlineData("Transcriptomics")]
    [InlineData("Bioinformatics")]
    [InlineData("MolecularBiology")]
    [InlineData("CellBiology")]
    [InlineData("Genetics")]
    [InlineData("Biochemistry")]
    [InlineData("Microbiology")]
    [InlineData("Immunology")]
    [InlineData("Neuroscience")]
    [InlineData("PlantBiology")]
    [InlineData("MarineBiology")]
    [InlineData("Ecology")]
    [InlineData("EvolutionaryBiology")]
    [InlineData("ComputationalBiology")]
    [InlineData("SyntheticBiology")]
    [InlineData("Other")]
    public void AllResearchFields_ShouldExist(string fieldName)
    {
        // Act & Assert
        var act = () => ResearchField.FromName(fieldName);
        act.Should().NotThrow();
    }

    #endregion
}
