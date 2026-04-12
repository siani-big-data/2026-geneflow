using GeneFlow.ApiNet2.Domain.Studies.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Studies.Enumerations;

/// <summary>
/// Unit tests for ResearchField smart enumeration.
/// </summary>
public class ResearchFieldTests
{
    #region Static Values

    [Fact]
    public void Genomics_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.Genomics.Id.Should().Be(1);
        ResearchField.Genomics.Name.Should().Be("Genomics");
        ResearchField.Genomics.DisplayName.Should().Be("Genomics");
    }

    [Fact]
    public void Proteomics_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.Proteomics.Id.Should().Be(2);
        ResearchField.Proteomics.Name.Should().Be("Proteomics");
        ResearchField.Proteomics.DisplayName.Should().Be("Proteomics");
    }

    [Fact]
    public void Transcriptomics_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.Transcriptomics.Id.Should().Be(3);
        ResearchField.Transcriptomics.Name.Should().Be("Transcriptomics");
        ResearchField.Transcriptomics.DisplayName.Should().Be("Transcriptomics");
    }

    [Fact]
    public void Metagenomics_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.Metagenomics.Id.Should().Be(4);
        ResearchField.Metagenomics.Name.Should().Be("Metagenomics");
        ResearchField.Metagenomics.DisplayName.Should().Be("Metagenomics");
    }

    [Fact]
    public void Phylogenetics_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.Phylogenetics.Id.Should().Be(5);
        ResearchField.Phylogenetics.Name.Should().Be("Phylogenetics");
        ResearchField.Phylogenetics.DisplayName.Should().Be("Phylogenetics");
    }

    [Fact]
    public void MolecularBiology_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.MolecularBiology.Id.Should().Be(6);
        ResearchField.MolecularBiology.Name.Should().Be("MolecularBiology");
        ResearchField.MolecularBiology.DisplayName.Should().Be("Molecular Biology");
    }

    [Fact]
    public void Genetics_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.Genetics.Id.Should().Be(7);
        ResearchField.Genetics.Name.Should().Be("Genetics");
        ResearchField.Genetics.DisplayName.Should().Be("Genetics");
    }

    [Fact]
    public void Bioinformatics_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.Bioinformatics.Id.Should().Be(8);
        ResearchField.Bioinformatics.Name.Should().Be("Bioinformatics");
        ResearchField.Bioinformatics.DisplayName.Should().Be("Bioinformatics");
    }

    [Fact]
    public void Other_ShouldHaveCorrectValues()
    {
        // Assert
        ResearchField.Other.Id.Should().Be(9);
        ResearchField.Other.Name.Should().Be("Other");
        ResearchField.Other.DisplayName.Should().Be("Other");
    }

    #endregion

    #region GetAll

    [Fact]
    public void GetAll_ShouldReturnAllFields()
    {
        // Act
        var allFields = ResearchField.GetAll();

        // Assert
        allFields.Should().HaveCount(9);
        allFields.Should().Contain(ResearchField.Genomics);
        allFields.Should().Contain(ResearchField.Proteomics);
        allFields.Should().Contain(ResearchField.Transcriptomics);
        allFields.Should().Contain(ResearchField.Metagenomics);
        allFields.Should().Contain(ResearchField.Phylogenetics);
        allFields.Should().Contain(ResearchField.MolecularBiology);
        allFields.Should().Contain(ResearchField.Genetics);
        allFields.Should().Contain(ResearchField.Bioinformatics);
        allFields.Should().Contain(ResearchField.Other);
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Genomics")]
    [InlineData(2, "Proteomics")]
    [InlineData(3, "Transcriptomics")]
    [InlineData(4, "Metagenomics")]
    [InlineData(5, "Phylogenetics")]
    [InlineData(6, "MolecularBiology")]
    [InlineData(7, "Genetics")]
    [InlineData(8, "Bioinformatics")]
    [InlineData(9, "Other")]
    public void FromId_WithValidId_ShouldReturnCorrectField(int id, string expectedName)
    {
        // Act
        var field = ResearchField.FromId(id);

        // Assert
        field.Should().NotBeNull();
        field!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var field = ResearchField.FromId(999);

        // Assert
        field.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Genomics", 1)]
    [InlineData("PROTEOMICS", 2)]
    [InlineData("transcriptomics", 3)]
    [InlineData("MolecularBiology", 6)]
    public void FromName_WithValidName_ShouldReturnCorrectField(string name, int expectedId)
    {
        // Act
        var field = ResearchField.FromName(name);

        // Assert
        field.Should().NotBeNull();
        field!.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var field = ResearchField.FromName("InvalidField");

        // Assert
        field.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameField_ShouldReturnTrue()
    {
        // Arrange
        var field1 = ResearchField.Genomics;
        var field2 = ResearchField.FromId(1);

        // Assert
        (field1 == field2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentField_ShouldReturnFalse()
    {
        // Arrange
        var field1 = ResearchField.Genomics;
        var field2 = ResearchField.Proteomics;

        // Assert
        (field1 != field2).Should().BeTrue();
    }

    #endregion

    #region DisplayName

    [Fact]
    public void DisplayName_MolecularBiology_ShouldHaveSpace()
    {
        // Assert
        ResearchField.MolecularBiology.DisplayName.Should().Contain(" ");
        ResearchField.MolecularBiology.DisplayName.Should().Be("Molecular Biology");
    }

    [Fact]
    public void DisplayName_SingleWord_ShouldMatchName()
    {
        // Assert
        ResearchField.Genomics.DisplayName.Should().Be(ResearchField.Genomics.Name);
        ResearchField.Proteomics.DisplayName.Should().Be(ResearchField.Proteomics.Name);
    }

    #endregion
}
