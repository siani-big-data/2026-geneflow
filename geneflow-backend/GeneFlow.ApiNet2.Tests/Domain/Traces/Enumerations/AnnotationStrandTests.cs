using GeneFlow.ApiNet2.Domain.Traces.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Enumerations;

/// <summary>
/// Unit tests for AnnotationStrand enumeration.
/// </summary>
public class AnnotationStrandTests
{
    #region Enumeration Values

    [Fact]
    public void AllAnnotationStrands_ShouldHaveUniqueIds()
    {
        // Arrange
        var strands = AnnotationStrand.GetAll();

        // Act
        var ids = strands.Select(s => s.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllAnnotationStrands_ShouldHaveUniqueNames()
    {
        // Arrange
        var strands = AnnotationStrand.GetAll();

        // Act
        var names = strands.Select(s => s.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Plus_ShouldHaveCorrectProperties()
    {
        // Assert
        AnnotationStrand.Plus.Id.Should().Be(1);
        AnnotationStrand.Plus.Name.Should().Be("Plus");
        AnnotationStrand.Plus.Symbol.Should().Be("+");
        AnnotationStrand.Plus.Description.Should().Be("Forward strand (5' to 3')");
    }

    [Fact]
    public void Minus_ShouldHaveCorrectProperties()
    {
        // Assert
        AnnotationStrand.Minus.Id.Should().Be(2);
        AnnotationStrand.Minus.Name.Should().Be("Minus");
        AnnotationStrand.Minus.Symbol.Should().Be("-");
        AnnotationStrand.Minus.Description.Should().Be("Reverse strand (3' to 5')");
    }

    [Fact]
    public void None_ShouldHaveCorrectProperties()
    {
        // Assert
        AnnotationStrand.None.Id.Should().Be(3);
        AnnotationStrand.None.Name.Should().Be("None");
        AnnotationStrand.None.Symbol.Should().Be(".");
        AnnotationStrand.None.Description.Should().Be("No strand orientation");
    }

    #endregion

    #region FromSymbol

    [Theory]
    [InlineData("+", "Plus")]
    [InlineData("-", "Minus")]
    [InlineData(".", "None")]
    [InlineData("", "None")]
    public void FromSymbol_ShouldReturnCorrectStrand(string symbol, string expectedName)
    {
        // Act
        var strand = AnnotationStrand.FromSymbol(symbol);

        // Assert
        strand.Should().NotBeNull();
        strand!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromSymbol_WithInvalidSymbol_ShouldReturnNull()
    {
        // Act
        var strand = AnnotationStrand.FromSymbol("x");

        // Assert
        strand.Should().BeNull();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Plus")]
    [InlineData(2, "Minus")]
    [InlineData(3, "None")]
    public void FromId_ShouldReturnCorrectStrand(int id, string expectedName)
    {
        // Act
        var strand = AnnotationStrand.FromId(id);

        // Assert
        strand.Should().NotBeNull();
        strand!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var strand = AnnotationStrand.FromId(999);

        // Assert
        strand.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Plus", 1)]
    [InlineData("Minus", 2)]
    [InlineData("None", 3)]
    public void FromName_ShouldReturnCorrectStrand(string name, int expectedId)
    {
        // Act
        var strand = AnnotationStrand.FromName(name);

        // Assert
        strand.Should().NotBeNull();
        strand!.Id.Should().Be(expectedId);
    }

    #endregion

    #region List

    [Fact]
    public void List_ShouldContainAllStrands()
    {
        // Act
        var strands = AnnotationStrand.GetAll();

        // Assert
        strands.Should().HaveCount(3);
        strands.Should().Contain(AnnotationStrand.Plus);
        strands.Should().Contain(AnnotationStrand.Minus);
        strands.Should().Contain(AnnotationStrand.None);
    }

    #endregion
}
