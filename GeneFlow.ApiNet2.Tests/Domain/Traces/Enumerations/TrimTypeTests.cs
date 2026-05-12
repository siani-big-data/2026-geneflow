using GeneFlow.ApiNet2.Domain.Traces.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Enumerations;

/// <summary>
/// Unit tests for TrimType enumeration.
/// </summary>
public class TrimTypeTests
{
    #region TrimType Values

    [Fact]
    public void AllTrimTypes_ShouldHaveUniqueIds()
    {
        // Arrange
        var types = TrimType.GetAll();

        // Act
        var ids = types.Select(t => t.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllTrimTypes_ShouldHaveUniqueNames()
    {
        // Arrange
        var types = TrimType.GetAll();

        // Act
        var names = types.Select(t => t.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Manual_ShouldHaveCorrectProperties()
    {
        // Assert
        TrimType.Manual.Id.Should().Be(1);
        TrimType.Manual.Name.Should().Be("Manual");
        TrimType.Manual.DisplayName.Should().Be("Manual Trim");
    }

    [Fact]
    public void AutoMott_ShouldHaveCorrectProperties()
    {
        // Assert
        TrimType.AutoMott.Id.Should().Be(2);
        TrimType.AutoMott.Name.Should().Be("AutoMott");
        TrimType.AutoMott.DisplayName.Should().Be("Mott Algorithm");
    }

    [Fact]
    public void AutoWindow_ShouldHaveCorrectProperties()
    {
        // Assert
        TrimType.AutoWindow.Id.Should().Be(3);
        TrimType.AutoWindow.Name.Should().Be("AutoWindow");
        TrimType.AutoWindow.DisplayName.Should().Be("Sliding Window");
    }

    #endregion

    #region IsAutomatic

    [Fact]
    public void Manual_IsAutomatic_ShouldBeFalse()
    {
        TrimType.Manual.IsAutomatic.Should().BeFalse();
    }

    [Fact]
    public void AutoMott_IsAutomatic_ShouldBeTrue()
    {
        TrimType.AutoMott.IsAutomatic.Should().BeTrue();
    }

    [Fact]
    public void AutoWindow_IsAutomatic_ShouldBeTrue()
    {
        TrimType.AutoWindow.IsAutomatic.Should().BeTrue();
    }

    [Fact]
    public void AutomaticTypes_ShouldHaveIsAutomaticTrue()
    {
        // Arrange
        var automaticTypes = TrimType.GetAll().Where(t => t.IsAutomatic);

        // Assert
        automaticTypes.Should().HaveCount(2);
        automaticTypes.Should().Contain(TrimType.AutoMott);
        automaticTypes.Should().Contain(TrimType.AutoWindow);
    }

    [Fact]
    public void ManualTypes_ShouldHaveIsAutomaticFalse()
    {
        // Arrange
        var manualTypes = TrimType.GetAll().Where(t => !t.IsAutomatic);

        // Assert
        manualTypes.Should().HaveCount(1);
        manualTypes.Should().Contain(TrimType.Manual);
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "Manual")]
    [InlineData(2, "AutoMott")]
    [InlineData(3, "AutoWindow")]
    public void FromId_ShouldReturnCorrectType(int id, string expectedName)
    {
        // Act
        var type = TrimType.FromId(id);

        // Assert
        type.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(1, "Manual Trim")]
    [InlineData(2, "Mott Algorithm")]
    [InlineData(3, "Sliding Window")]
    public void FromId_ShouldReturnCorrectDisplayName(int id, string expectedDisplayName)
    {
        // Act
        var type = TrimType.FromId(id);

        // Assert
        type.DisplayName.Should().Be(expectedDisplayName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = TrimType.FromId(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("Manual", 1)]
    [InlineData("AutoMott", 2)]
    [InlineData("AutoWindow", 3)]
    public void FromName_ShouldReturnCorrectType(string name, int expectedId)
    {
        // Act
        var type = TrimType.FromName(name);

        // Assert
        type.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var result = TrimType.FromName("InvalidName");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region List

    [Fact]
    public void List_ShouldContainAllTypes()
    {
        // Act
        var types = TrimType.GetAll();

        // Assert
        types.Should().HaveCount(3);
        types.Should().Contain(TrimType.Manual);
        types.Should().Contain(TrimType.AutoMott);
        types.Should().Contain(TrimType.AutoWindow);
    }

    [Fact]
    public void List_ShouldBeOrderedById()
    {
        // Act
        var types = TrimType.GetAll().ToList();

        // Assert
        types[0].Should().Be(TrimType.Manual);
        types[1].Should().Be(TrimType.AutoMott);
        types[2].Should().Be(TrimType.AutoWindow);
    }

    #endregion

    #region Equality

    [Fact]
    public void SameEnumValues_ShouldBeEqual()
    {
        // Arrange
        var type1 = TrimType.Manual;
        var type2 = TrimType.FromId(1);

        // Assert
        type1.Should().Be(type2);
    }

    [Fact]
    public void DifferentEnumValues_ShouldNotBeEqual()
    {
        // Assert
        TrimType.Manual.Should().NotBe(TrimType.AutoMott);
        TrimType.AutoMott.Should().NotBe(TrimType.AutoWindow);
    }

    #endregion

    #region DisplayName

    [Fact]
    public void AllTrimTypes_ShouldHaveNonEmptyDisplayNames()
    {
        // Act
        var types = TrimType.GetAll();

        // Assert
        foreach (var type in types)
        {
            type.DisplayName.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void AllTrimTypes_ShouldHaveUniqueDisplayNames()
    {
        // Arrange
        var types = TrimType.GetAll();

        // Act
        var displayNames = types.Select(t => t.DisplayName).ToList();

        // Assert
        displayNames.Should().OnlyHaveUniqueItems();
    }

    #endregion
}
