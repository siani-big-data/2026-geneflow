using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.ValueObjects;

/// <summary>
/// Unit tests for the Institution value object.
/// </summary>
public class InstitutionTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidName_ShouldCreate()
    {
        // Arrange
        var name = "MIT";

        // Act
        var result = Institution.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("MIT");
        result.Value.Department.Should().BeNull();
    }

    [Fact]
    public void Create_WithNameAndDepartment_ShouldSetBoth()
    {
        // Arrange
        var name = "MIT";
        var department = "Biology";

        // Act
        var result = Institution.Create(name, department);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("MIT");
        result.Value.Department.Should().Be("Biology");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var name = "  MIT  ";
        var department = "  Biology Department  ";

        // Act
        var result = Institution.Create(name, department);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("MIT");
        result.Value.Department.Should().Be("Biology Department");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldReturnEmptyInstitution(string? name)
    {
        // Act
        var result = Institution.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().BeNull();
        result.Value.Department.Should().BeNull();
    }

    #endregion

    #region Create - Validation Failures

    [Fact]
    public void Create_WithNameTooShort_ShouldReturnError()
    {
        // Arrange
        var name = "A"; // Less than MinLength (2)

        // Act
        var result = Institution.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InstitutionNameTooShort");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldReturnError()
    {
        // Arrange
        var name = new string('A', Institution.NameMaxLength + 1);

        // Act
        var result = Institution.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InstitutionNameTooLong");
    }

    [Fact]
    public void Create_WithDepartmentTooShort_ShouldReturnError()
    {
        // Arrange
        var name = "MIT";
        var department = "A"; // Less than MinLength (2)

        // Act
        var result = Institution.Create(name, department);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InstitutionDepartmentTooShort");
    }

    [Fact]
    public void Create_WithDepartmentTooLong_ShouldReturnError()
    {
        // Arrange
        var name = "MIT";
        var department = new string('A', Institution.DepartmentMaxLength + 1);

        // Act
        var result = Institution.Create(name, department);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InstitutionDepartmentTooLong");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldReturnNullValues()
    {
        // Act
        var empty = Institution.Empty;

        // Assert
        empty.Name.Should().BeNull();
        empty.Department.Should().BeNull();
        empty.DisplayName.Should().BeNull();
    }

    #endregion

    #region DisplayName

    [Fact]
    public void DisplayName_WithOnlyName_ShouldReturnName()
    {
        // Arrange
        var result = Institution.Create("MIT");

        // Assert
        result.Value.DisplayName.Should().Be("MIT");
    }

    [Fact]
    public void DisplayName_WithNameAndDepartment_ShouldReturnCombined()
    {
        // Arrange
        var result = Institution.Create("MIT", "Biology");

        // Assert
        result.Value.DisplayName.Should().Be("MIT, Biology");
    }

    [Fact]
    public void DisplayName_WithEmptyInstitution_ShouldReturnNull()
    {
        // Arrange
        var empty = Institution.Empty;

        // Assert
        empty.DisplayName.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_SameNameAndDepartment_ShouldBeEqual()
    {
        // Arrange
        var institution1 = Institution.Create("MIT", "Biology").Value;
        var institution2 = Institution.Create("MIT", "Biology").Value;

        // Assert
        institution1.Should().Be(institution2);
        (institution1 == institution2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentName_ShouldNotBeEqual()
    {
        // Arrange
        var institution1 = Institution.Create("MIT").Value;
        var institution2 = Institution.Create("Harvard").Value;

        // Assert
        institution1.Should().NotBe(institution2);
    }

    [Fact]
    public void Equality_SameNameDifferentDepartment_ShouldNotBeEqual()
    {
        // Arrange
        var institution1 = Institution.Create("MIT", "Biology").Value;
        var institution2 = Institution.Create("MIT", "Physics").Value;

        // Assert
        institution1.Should().NotBe(institution2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WithValue_ShouldReturnDisplayName()
    {
        // Arrange
        var institution = Institution.Create("MIT", "Biology").Value;

        // Assert
        institution.ToString().Should().Be("MIT, Biology");
    }

    [Fact]
    public void ToString_Empty_ShouldReturnEmptyString()
    {
        // Arrange
        var empty = Institution.Empty;

        // Assert
        empty.ToString().Should().BeEmpty();
    }

    #endregion
}
