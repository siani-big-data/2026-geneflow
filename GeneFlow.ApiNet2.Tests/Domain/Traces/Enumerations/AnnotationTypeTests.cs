using GeneFlow.ApiNet2.Domain.Traces.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Enumerations;

/// <summary>
/// Unit tests for AnnotationType enumeration.
/// </summary>
public class AnnotationTypeTests
{
    #region Enumeration Values

    [Fact]
    public void AllAnnotationTypes_ShouldHaveUniqueIds()
    {
        // Arrange
        var types = AnnotationType.GetAll();

        // Act
        var ids = types.Select(t => t.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllAnnotationTypes_ShouldHaveUniqueNames()
    {
        // Arrange
        var types = AnnotationType.GetAll();

        // Act
        var names = types.Select(t => t.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Region_ShouldHaveCorrectProperties()
    {
        // Assert
        AnnotationType.Region.Id.Should().Be(1);
        AnnotationType.Region.Name.Should().Be("Region");
        AnnotationType.Region.DisplayName.Should().Be("Region");
        AnnotationType.Region.IsRange.Should().BeTrue();
    }

    [Fact]
    public void Point_ShouldHaveCorrectProperties()
    {
        // Assert
        AnnotationType.Point.Id.Should().Be(2);
        AnnotationType.Point.Name.Should().Be("Point");
        AnnotationType.Point.DisplayName.Should().Be("Point");
        AnnotationType.Point.IsRange.Should().BeFalse();
    }

    [Fact]
    public void FeatuREDACTED()
    {
        // Assert
        AnnotationType.Feature.Id.Should().Be(3);
        AnnotationType.Feature.Name.Should().Be("Feature");
        AnnotationType.Feature.DisplayName.Should().Be("Feature");
        AnnotationType.Feature.IsRange.Should().BeTrue();
    }

    [Fact]
    public void Custom_ShouldHaveCorrectProperties()
    {
        // Assert
        AnnotationType.Custom.Id.Should().Be(4);
        AnnotationType.Custom.Name.Should().Be("Custom");
        AnnotationType.Custom.DisplayName.Should().Be("Custom");
        AnnotationType.Custom.IsRange.Should().BeTrue();
    }

    #endregion

    #region IsRange Property

    [Fact]
    public void Point_IsRange_ShouldBeFalse()
    {
        // Assert
        AnnotationType.Point.IsRange.Should().BeFalse();
    }

    [Theory]
    [InlineData("Region")]
    [InlineData("Feature")]
    [InlineData("Custom")]
    public void RangeTypes_IsRange_ShouldBeTrue(string typeName)
    {
        // Arrange
        var type = AnnotationType.FromName(typeName);

        // Assert
        type.IsRange.Should().BeTrue();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Region")]
    [InlineData(2, "Point")]
    [InlineData(3, "Feature")]
    [InlineData(4, "Custom")]
    public void FromId_ShouldReturnCorrectType(int id, string expectedName)
    {
        // Act
        var type = AnnotationType.FromId(id);

        // Assert
        type.Should().NotBeNull();
        type!.Name.Should().Be(expectedName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var type = AnnotationType.FromId(999);

        // Assert
        type.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Region", 1)]
    [InlineData("Point", 2)]
    [InlineData("Feature", 3)]
    [InlineData("Custom", 4)]
    public void FromName_ShouldReturnCorrectType(string name, int expectedId)
    {
        // Act
        var type = AnnotationType.FromName(name);

        // Assert
        type.Should().NotBeNull();
        type!.Id.Should().Be(expectedId);
    }

    #endregion

    #region List

    [Fact]
    public void List_ShouldContainAllTypes()
    {
        // Act
        var types = AnnotationType.GetAll();

        // Assert
        types.Should().HaveCount(4);
        types.Should().Contain(AnnotationType.Region);
        types.Should().Contain(AnnotationType.Point);
        types.Should().Contain(AnnotationType.Feature);
        types.Should().Contain(AnnotationType.Custom);
    }

    #endregion
}
