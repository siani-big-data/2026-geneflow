using GeneFlow.ApiNet2.Domain.Traces.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Domain.Traces.Enumerations;

/// <summary>
/// Unit tests for TrimEnd enumeration.
/// </summary>
public class TrimEndTests
{
    #region TrimEnd Values

    [Fact]
    public void AllTrimEnds_ShouldHaveUniqueIds()
    {
        // Arrange
        var ends = TrimEnd.GetAll();

        // Act
        var ids = ends.Select(e => e.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void AllTrimEnds_ShouldHaveUniqueNames()
    {
        // Arrange
        var ends = TrimEnd.GetAll();

        // Act
        var names = ends.Select(e => e.Name).ToList();

        // Assert
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void FivePrime_ShouldHaveCorrectProperties()
    {
        // Assert
        TrimEnd.FivePrime.Id.Should().Be(1);
        TrimEnd.FivePrime.Name.Should().Be("FivePrime");
        TrimEnd.FivePrime.DisplayName.Should().Be("5' End");
    }

    [Fact]
    public void ThreePrime_ShouldHaveCorrectProperties()
    {
        // Assert
        TrimEnd.ThreePrime.Id.Should().Be(2);
        TrimEnd.ThreePrime.Name.Should().Be("ThreePrime");
        TrimEnd.ThreePrime.DisplayName.Should().Be("3' End");
    }

    #endregion

    #region FromId

    [Theory]
    [InlineData(1, "FivePrime")]
    [InlineData(2, "ThreePrime")]
    public void FromId_ShouldReturnCorrectType(int id, string expectedName)
    {
        // Act
        var end = TrimEnd.FromId(id);

        // Assert
        end.Name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(1, "5' End")]
    [InlineData(2, "3' End")]
    public void FromId_ShouldReturnCorrectDisplayName(int id, string expectedDisplayName)
    {
        // Act
        var end = TrimEnd.FromId(id);

        // Assert
        end.DisplayName.Should().Be(expectedDisplayName);
    }

    [Fact]
    public void FromId_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = TrimEnd.FromId(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region FromName

    [Theory]
    [InlineData("FivePrime", 1)]
    [InlineData("ThreePrime", 2)]
    public void FromName_ShouldReturnCorrectType(string name, int expectedId)
    {
        // Act
        var end = TrimEnd.FromName(name);

        // Assert
        end.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FromName_WithInvalidName_ShouldReturnNull()
    {
        // Act
        var result = TrimEnd.FromName("InvalidName");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region List

    [Fact]
    public void List_ShouldContainAllTypes()
    {
        // Act
        var ends = TrimEnd.GetAll();

        // Assert
        ends.Should().HaveCount(2);
        ends.Should().Contain(TrimEnd.FivePrime);
        ends.Should().Contain(TrimEnd.ThreePrime);
    }

    [Fact]
    public void List_ShouldBeOrderedById()
    {
        // Act
        var ends = TrimEnd.GetAll().ToList();

        // Assert
        ends[0].Should().Be(TrimEnd.FivePrime);
        ends[1].Should().Be(TrimEnd.ThreePrime);
    }

    #endregion

    #region Equality

    [Fact]
    public void SameEnumValues_ShouldBeEqual()
    {
        // Arrange
        var end1 = TrimEnd.FivePrime;
        var end2 = TrimEnd.FromId(1);

        // Assert
        end1.Should().Be(end2);
    }

    [Fact]
    public void DifferentEnumValues_ShouldNotBeEqual()
    {
        // Assert
        TrimEnd.FivePrime.Should().NotBe(TrimEnd.ThreePrime);
    }

    #endregion

    #region DisplayName

    [Fact]
    public void AllTrimEnds_ShouldHaveNonEmptyDisplayNames()
    {
        // Act
        var ends = TrimEnd.GetAll();

        // Assert
        foreach (var end in ends)
        {
            end.DisplayName.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void AllTrimEnds_ShouldHaveUniqueDisplayNames()
    {
        // Arrange
        var ends = TrimEnd.GetAll();

        // Act
        var displayNames = ends.Select(e => e.DisplayName).ToList();

        // Assert
        displayNames.Should().OnlyHaveUniqueItems();
    }

    #endregion
}
