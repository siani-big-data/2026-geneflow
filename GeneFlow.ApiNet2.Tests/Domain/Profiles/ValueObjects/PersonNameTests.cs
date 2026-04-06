using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.ValueObjects;

/// <summary>
/// Unit tests for the PersonName value object.
/// </summary>
public class PersonNameTests
{
    #region Create - Valid Cases

    [Theory]
    [InlineData("John", null, "John", "J")]
    [InlineData("John", "Doe", "John Doe", "JD")]
    [InlineData("María", "García", "María García", "MG")]
    [InlineData("Jean-Pierre", "O'Connor", "Jean-Pierre O'Connor", "JO")]
    public void Create_WithValidNames_ShouldReturnSuccess(
        string firstName,
        string? lastName,
        string expectedFullName,
        string expectedInitials)
    {
        // Act
        var result = PersonName.Create(firstName, lastName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be(firstName);
        result.Value.LastName.Should().Be(lastName);
        result.Value.FullName.Should().Be(expectedFullName);
        result.Value.Initials.Should().Be(expectedInitials);
    }

    [Fact]
    public void Create_WithOnlyFirstName_ShouldHaveSingleInitial()
    {
        // Act
        var result = PersonName.Create("Alice");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Initials.Should().Be("A");
        result.Value.FullName.Should().Be("Alice");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Act
        var result = PersonName.Create("  John  ", "  Doe  ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
    }

    [Fact]
    public void Create_WithWhitespaceLastName_ShouldTreatAsNull()
    {
        // Act
        var result = PersonName.Create("John", "   ");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LastName.Should().BeNull();
    }

    #endregion

    #region Create - Invalid Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyFirstName_ShouldReturnFailure(string? firstName)
    {
        // Act
        var result = PersonName.Create(firstName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FirstName");
    }

    [Fact]
    public void Create_WithFirstNameTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longFirstName = new string('a', PersonName.FirstNameMaxLength + 1);

        // Act
        var result = PersonName.Create(longFirstName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("FirstName");
    }

    [Fact]
    public void Create_WithLastNameTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longLastName = new string('a', PersonName.LastNameMaxLength + 1);

        // Act
        var result = PersonName.Create("John", longLastName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("LastName");
    }

    [Theory]
    [InlineData("John123")]
    [InlineData("John@Doe")]
    [InlineData("John#")]
    public void Create_WithInvalidFirstNameCharacters_ShouldReturnFailure(string firstName)
    {
        // Act
        var result = PersonName.Create(firstName);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("Doe123")]
    [InlineData("Doe@")]
    public void Create_WithInvalidLastNameCharacters_ShouldReturnFailure(string lastName)
    {
        // Act
        var result = PersonName.Create("John", lastName);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var name1 = PersonName.Create("John", "Doe").Value;
        var name2 = PersonName.Create("John", "Doe").Value;

        // Assert
        name1.Should().Be(name2);
    }

    [Fact]
    public void Equals_WithDifferentFirstName_ShouldReturnFalse()
    {
        // Arrange
        var name1 = PersonName.Create("John", "Doe").Value;
        var name2 = PersonName.Create("Jane", "Doe").Value;

        // Assert
        name1.Should().NotBe(name2);
    }

    [Fact]
    public void Equals_WithDifferentLastName_ShouldReturnFalse()
    {
        // Arrange
        var name1 = PersonName.Create("John", "Doe").Value;
        var name2 = PersonName.Create("John", "Smith").Value;

        // Assert
        name1.Should().NotBe(name2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnFullName()
    {
        // Arrange
        var name = PersonName.Create("John", "Doe").Value;

        // Act
        var result = name.ToString();

        // Assert
        result.Should().Be("John Doe");
    }

    #endregion
}
