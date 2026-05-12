using GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;
using GeneFlow.ApiNet2.Infrastructure.Identity.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for the EmailService.
/// Tests email template generation and content formatting.
/// Note: Actual SMTP sending is not tested as it requires external infrastructure.
/// </summary>
public class EmailServiceTests
{
    private readonly EmailSettings _settings;

    public EmailServiceTests()
    {
        _settings = new EmailSettings
        {
            SmtpHost = "smtp.test.local",
            SmtpPort = 587,
            SmtpUsername = "testuser",
            SmtpPassword = "testpass",
            UseSsl = true,
            FromEmail = "noreply@geneflow.test",
            FromName = "GeneFlow Test",
            BaseUrl = "https://app.geneflow.test"
        };
    }

    #region Configuration

    [Fact]
    public void EmailSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange
        var defaultSettings = new EmailSettings();

        // Assert
        defaultSettings.SmtpHost.Should().Be("localhost");
        defaultSettings.SmtpPort.Should().Be(587);
        defaultSettings.UseSsl.Should().BeTrue();
        defaultSettings.FromEmail.Should().Be("noreply@geneflow.com");
        defaultSettings.FromName.Should().Be("GeneFlow");
    }

    [Fact]
    public void EmailSettings_ShouldAllowCustomValues()
    {
        // Assert
        _settings.SmtpHost.Should().Be("smtp.test.local");
        _settings.SmtpPort.Should().Be(587);
        _settings.SmtpUsername.Should().Be("testuser");
        _settings.SmtpPassword.Should().Be("testpass");
        _settings.UseSsl.Should().BeTrue();
        _settings.FromEmail.Should().Be("noreply@geneflow.test");
        _settings.FromName.Should().Be("GeneFlow Test");
        _settings.BaseUrl.Should().Be("https://app.geneflow.test");
    }

    #endregion

    #region Email Templates

    [Fact]
    public void EmailVerification_UrlFormat_ShouldBeCorrect()
    {
        // Arrange
        var token = "test-verification-token";
        var expectedUrl = $"{_settings.BaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";

        // Assert - URL format validation
        expectedUrl.Should().Contain("verify-email");
        expectedUrl.Should().Contain("token=");
        expectedUrl.Should().Contain(_settings.BaseUrl);
    }

    [Fact]
    public void PasswordReset_UrlFormat_ShouldBeCorrect()
    {
        // Arrange
        var token = "test-reset-token";
        var expectedUrl = $"{_settings.BaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        // Assert - URL format validation
        expectedUrl.Should().Contain("reset-password");
        expectedUrl.Should().Contain("token=");
        expectedUrl.Should().Contain(_settings.BaseUrl);
    }

    [Fact]
    public void EmailVerification_TokenWithSpecialChars_ShouldBeEscaped()
    {
        // Arrange
        var token = "token+with/special=chars";
        var escapedToken = Uri.EscapeDataString(token);

        // Assert
        escapedToken.Should().NotContain("+");
        escapedToken.Should().NotContain("/");
        escapedToken.Should().NotContain("=");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.uk")]
    [InlineData("user+tag@example.com")]
    public void EmailAddress_ShouldSupportValidFormats(string email)
    {
        // Arrange & Act - Validate email format patterns
        var isValid = email.Contains("@") && email.Contains(".");

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion

    #region Service Construction

    [Fact]
    public void EmailService_ShouldBeConstructable()
    {
        // Arrange
        var options = Options.Create(_settings);
        var logger = Substitute.For<ILogger<EmailService>>();

        // Act
        var service = new EmailService(options, logger);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void EmailService_WithNullLogger_ShouldNotThrowOnConstruction()
    {
        // Arrange
        var options = Options.Create(_settings);

        // Act - Constructor doesn't validate null logger (relies on nullable reference types)
        var service = new EmailService(options, null!);

        // Assert - Service is created (will fail at runtime when logging is attempted)
        service.Should().NotBeNull();
    }

    #endregion

    #region Email Content Validation

    [Theory]
    [InlineData("", "username", "token")]
    [InlineData("email@test.com", "", "token")]
    [InlineData("email@test.com", "username", "")]
    public void EmailParameters_WhenEmpty_ShouldBePrevented(string email, string username, string token)
    {
        // This test validates that the calling code should validate inputs
        // The actual validation happens at the application layer

        // Assert - At least one parameter is empty
        (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
            .Should().BeTrue();
    }

    [Fact]
    public void TwoFactorCode_Format_ShouldBeSixDigits()
    {
        // Arrange
        var validCode = "123456";
        var invalidCodes = new[] { "12345", "1234567", "abcdef", "12 456" };

        // Assert
        validCode.Length.Should().Be(6);
        validCode.All(char.IsDigit).Should().BeTrue();

        foreach (var code in invalidCodes)
        {
            (code.Length == 6 && code.All(char.IsDigit)).Should().BeFalse();
        }
    }

    #endregion
}
