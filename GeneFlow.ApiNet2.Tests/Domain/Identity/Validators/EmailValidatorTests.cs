using GeneFlow.ApiNet2.Domain.Identity.Validators;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Validators;

/// <summary>
/// Unit tests for the EmailValidator class.
/// </summary>
public class EmailValidatorTests
{
    private readonly EmailValidator _validator = new();

    #region Valid Email Tests

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@example.org")]
    [InlineData("firstname.lastname@company.com")]
    [InlineData("email@subdomain.domain.com")]
    [InlineData("user123@test.io")]
    public void Validate_WithValidEmail_ShouldReturnSuccess(string email)
    {
        // Act
        var result = _validator.Validate(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    public void Validate_WithMixedCaseEmail_ShouldNormalizeToLowercase()
    {
        // Arrange
        const string email = "Test.User@EXAMPLE.COM";

        // Act
        var result = _validator.Validate(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test.user@example.com");
    }

    [Fact]
    public void Validate_WithLeadingAndTrailingWhitespace_ShouldTrim()
    {
        // Arrange
        const string email = "  test@example.com  ";

        // Act
        var result = _validator.Validate(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test@example.com");
    }

    #endregion

    #region Null and Empty Tests

    [Fact]
    public void Validate_WithNullEmail_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailRequired");
    }

    [Fact]
    public void Validate_WithEmptyEmail_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailRequired");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyEmail_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate("   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailRequired");
    }

    #endregion

    #region Invalid Format Tests

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    [InlineData("double@@at.com")]
    [InlineData("nodot@domaincom")]
    public void Validate_WithInvalidFormat_ShouldReturnFailure(string email)
    {
        // Act
        var result = _validator.Validate(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailInvalidFormat");
    }

    [Fact]
    public void Validate_WithTooLongEmail_ShouldReturnFailure()
    {
        // Arrange
        var longEmail = new string('a', Email.MaxLength) + "@example.com";

        // Act
        var result = _validator.Validate(longEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailTooLong");
    }

    #endregion

    #region Domain Validation Tests

    [Theory]
    [InlineData("user@a.co")]  // Short but valid TLD
    [InlineData("user@domain.travel")]  // Long TLD
    [InlineData("user@sub.domain.example.com")]  // Multiple subdomains
    public void Validate_WithValidDomains_ShouldReturnSuccess(string email)
    {
        // Act
        var result = _validator.Validate(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("user@.com")]  // Missing domain name
    [InlineData("user@domain.")]  // Missing TLD
    [InlineData("user@domain.c")]  // TLD too short (single char)
    public void Validate_WithInvalidDomain_ShouldReturnFailure(string email)
    {
        // Act
        var result = _validator.Validate(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailInvalidFormat");
    }

    #endregion
}
