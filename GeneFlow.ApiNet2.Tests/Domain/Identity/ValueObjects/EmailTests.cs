using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the Email value object.
/// </summary>
public class EmailTests
{
    #region Create - Valid Cases

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@example.org")]
    [InlineData("firstname.lastname@company.com")]
    [InlineData("email@subdomain.domain.com")]
    public void Create_WithValidEmail_ShouldReturnSuccess(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(email);
    }

    #endregion

    #region Create - Invalid Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldReturnFailure(string? email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    [InlineData("double@@at.com")]
    public void Create_WithInvalidFormat_ShouldReturnFailure(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithTooLongEmail_ShouldReturnFailure()
    {
        // Arrange
        var longEmail = new string('a', Email.MaxLength) + "@example.com";

        // Act
        var result = Email.Create(longEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com").Value;
        var email2 = Email.Create("test@example.com").Value;

        // Assert
        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com").Value;
        var email2 = Email.Create("test2@example.com").Value;

        // Assert
        email1.Should().NotBe(email2);
        (email1 != email2).Should().BeTrue();
    }

    #endregion

    #region ToString and Implicit Conversion

    [Fact]
    public void ToString_ShouldReturnEmailValue()
    {
        // Arrange
        const string emailValue = "test@example.com";
        var email = Email.Create(emailValue).Value;

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be(emailValue);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnEmailValue()
    {
        // Arrange
        const string emailValue = "test@example.com";
        var email = Email.Create(emailValue).Value;

        // Act
        string result = email;

        // Assert
        result.Should().Be(emailValue);
    }

    #endregion
}
