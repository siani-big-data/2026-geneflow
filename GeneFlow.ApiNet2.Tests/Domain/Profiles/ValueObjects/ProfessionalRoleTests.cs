using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Profiles.ValueObjects;

/// <summary>
/// Unit tests for the ProfessionalRole value object.
/// </summary>
public class ProfessionalRoleTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidTitle_ShouldCreate()
    {
        // Arrange
        var title = "Senior Research Scientist";

        // Act
        var result = ProfessionalRole.Create(title);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Senior Research Scientist");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var title = "  Research Associate  ";

        // Act
        var result = ProfessionalRole.Create(title);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Research Associate");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_ShouldReturnEmptyRole(string? title)
    {
        // Act
        var result = ProfessionalRole.Create(title);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().BeNull();
    }

    [Theory]
    [InlineData("PhD Student")]
    [InlineData("Postdoctoral Researcher")]
    [InlineData("Lab Technician")]
    [InlineData("Professor")]
    [InlineData("Principal Investigator")]
    public void Create_CommonRoles_ShouldBeValid(string title)
    {
        // Act
        var result = ProfessionalRole.Create(title);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(title);
    }

    #endregion

    #region Create - Validation Failures

    [Fact]
    public void Create_TooShort_ShouldReturnError()
    {
        // Arrange
        var title = "A"; // Less than MinLength (2)

        // Act
        var result = ProfessionalRole.Create(title);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ProfessionalRoleTooShort");
    }

    [Fact]
    public void Create_TooLong_ShouldReturnError()
    {
        // Arrange
        var title = new string('A', ProfessionalRole.MaxLength + 1);

        // Act
        var result = ProfessionalRole.Create(title);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("ProfessionalRoleTooLong");
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldReturnNullValue()
    {
        // Act
        var empty = ProfessionalRole.Empty;

        // Assert
        empty.Value.Should().BeNull();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_SameTitle_ShouldBeEqual()
    {
        // Arrange
        var role1 = ProfessionalRole.Create("Research Associate").Value;
        var role2 = ProfessionalRole.Create("Research Associate").Value;

        // Assert
        role1.Should().Be(role2);
        (role1 == role2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentTitle_ShouldNotBeEqual()
    {
        // Arrange
        var role1 = ProfessionalRole.Create("Scientist").Value;
        var role2 = ProfessionalRole.Create("Researcher").Value;

        // Assert
        role1.Should().NotBe(role2);
    }

    [Fact]
    public void Equality_EmptyRoles_ShouldBeEqual()
    {
        // Arrange
        var role1 = ProfessionalRole.Empty;
        var role2 = ProfessionalRole.Create(null).Value;

        // Assert
        role1.Should().Be(role2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_WithValue_ShouldReturnValue()
    {
        // Arrange
        var role = ProfessionalRole.Create("Data Scientist").Value;

        // Assert
        role.ToString().Should().Be("Data Scientist");
    }

    [Fact]
    public void ToString_Empty_ShouldReturnEmptyString()
    {
        // Arrange
        var empty = ProfessionalRole.Empty;

        // Assert
        empty.ToString().Should().BeEmpty();
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ShouldConvertToString()
    {
        // Arrange
        var role = ProfessionalRole.Create("Bioinformatician").Value;

        // Act
        string? value = role;

        // Assert
        value.Should().Be("Bioinformatician");
    }

    [Fact]
    public void ImplicitConversion_Empty_ShouldReturnNull()
    {
        // Arrange
        var empty = ProfessionalRole.Empty;

        // Act
        string? value = empty;

        // Assert
        value.Should().BeNull();
    }

    #endregion

    #region Constants

    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        // Assert
        ProfessionalRole.MinLength.Should().Be(2);
        ProfessionalRole.MaxLength.Should().Be(100);
    }

    #endregion
}
