using GeneFlow.ApiNet2.Domain.Traces.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Enumerations;

/// <summary>
/// Unit tests for EditType enumeration.
/// </summary>
public class EditTypeTests
{
    #region Edit Type Values

    [Fact]
    public void AllEditTypes_ShouldHaveUniqueIds()
    {
        // Arrange
        var types = EditType.GetAll();

        // Act
        var ids = types.Select(t => t.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllEditTypes_ShouldHaveUniqueNames()
    {
        // Arrange
        var types = EditType.GetAll();

        // Act
        var names = types.Select(t => t.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Change_ShouldHaveCorrectProperties()
    {
        // Assert
        EditType.Change.Id.Should().Be(1);
        EditType.Change.Name.Should().Be("Change");
        EditType.Change.DisplayName.Should().Be("Base Change");
    }

    [Fact]
    public void Insert_ShouldHaveCorrectProperties()
    {
        // Assert
        EditType.Insert.Id.Should().Be(2);
        EditType.Insert.Name.Should().Be("Insert");
        EditType.Insert.DisplayName.Should().Be("Base Insertion");
    }

    [Fact]
    public void Delete_ShouldHaveCorrectProperties()
    {
        // Assert
        EditType.Delete.Id.Should().Be(3);
        EditType.Delete.Name.Should().Be("Delete");
        EditType.Delete.DisplayName.Should().Be("Base Deletion");
    }

    #endregion

    #region RequiresOriginalBase

    [Fact]
    public void Change_RequiresOriginalBase_ShouldBeTrue()
    {
        EditType.Change.RequiresOriginalBase.Should().BeTrue();
    }

    [Fact]
    public void Insert_RequiresOriginalBase_ShouldBeFalse()
    {
        EditType.Insert.RequiresOriginalBase.Should().BeFalse();
    }

    [Fact]
    public void Delete_RequiresOriginalBase_ShouldBeTrue()
    {
        EditType.Delete.RequiresOriginalBase.Should().BeTrue();
    }

    #endregion

    #region RequiresNewBase

    [Fact]
    public void Change_RequiresNewBase_ShouldBeTrue()
    {
        EditType.Change.RequiresNewBase.Should().BeTrue();
    }

    [Fact]
    public void Insert_RequiresNewBase_ShouldBeTrue()
    {
        EditType.Insert.RequiresNewBase.Should().BeTrue();
    }

    [Fact]
    public void Delete_RequiresNewBase_ShouldBeFalse()
    {
        EditType.Delete.RequiresNewBase.Should().BeFalse();
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Change")]
    [InlineData(2, "Insert")]
    [InlineData(3, "Delete")]
    public void FromId_ShouldReturnCorrectType(int id, string expectedName)
    {
        // Act
        var type = EditType.FromId(id);

        // Assert
        type.Name.Should().Be(expectedName);
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Change", 1)]
    [InlineData("Insert", 2)]
    [InlineData("Delete", 3)]
    public void FromName_ShouldReturnCorrectType(string name, int expectedId)
    {
        // Act
        var type = EditType.FromName(name);

        // Assert
        type.Id.Should().Be(expectedId);
    }

    #endregion

    #region List

    [Fact]
    public void List_ShouldContainAllTypes()
    {
        // Act
        var types = EditType.GetAll();

        // Assert
        types.Should().HaveCount(3);
        types.Should().Contain(EditType.Change);
        types.Should().Contain(EditType.Insert);
        types.Should().Contain(EditType.Delete);
    }

    #endregion
}
