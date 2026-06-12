using System.IdentityModel.Tokens.Jwt;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.Infrastructure.Identity.Configuration;
using GeneFlow.ApiNet2.Infrastructure.Identity.Services;
using Microsoft.Extensions.Options;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Identity.Services;

/// <summary>
/// Unit tests for the JwtTokenGenerator service.
/// </summary>
public class JwtTokenGeneratorTests
{
    private readonly JwtSettings _settings;
    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        _settings = new JwtSettings
        {
            Secret = "ThisIsAVeryLongSecretKeyForTestingPurposesAtLeast32Chars!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        var options = Options.Create(_settings);
        _generator = new JwtTokenGenerator(options);
    }

    #region Helper Methods

    private static User CreateTestUser()
    {
        var userId = new UserId(1);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        var user = User.Create(userId, email, username, passwordHash).Value;
        user.VerifyEmail(user.EmailVerificationToken!);
        return user;
    }

    #endregion

    #region GenerateAccessToken

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var (token, expiresAt) = _generator.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetCorrectExpiration()
    {
        // Arrange
        var user = CreateTestUser();
        var expectedExpiry = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);

        // Act
        var (_, expiresAt) = _generator.GenerateAccessToken(user);

        // Assert
        expiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainUserClaims()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var (token, _) = _generator.GenerateAccessToken(user);

        // Decode and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert - Check key claims are present
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Name || c.Type == "name");
        jwtToken.Claims.Should().Contain(c => c.Type == "email_verified");
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainEmailVerifiedClaim()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var (token, _) = _generator.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Claims.Should().Contain(c => c.Type == "email_verified" && c.Value == "true");
    }

    [Fact]
    public void GenerateAccessToken_WithMultipleRoles_ShouldGenerateToken()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRole(Role.Admin);

        // Act
        var (token, expiresAt) = _generator.GenerateAccessToken(user);

        // Assert - Token should be generated successfully
        token.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeAfter(DateTime.UtcNow);

        // User should have the roles set (this tests the domain model, not the JWT directly)
        user.Roles.Should().HaveCount(2);
        user.HasRole(Role.User).Should().BeTrue();
        user.HasRole(Role.Admin).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ShouldBeDecodable()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var (token, _) = _generator.GenerateAccessToken(user);

        // Assert - Token should be decodable
        var handler = new JwtSecurityTokenHandler();
        var canRead = handler.CanReadToken(token);
        canRead.Should().BeTrue();

        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Should().NotBeNull();
        // Note: The token has 3 parts (header.payload.signature)
        token.Split('.').Should().HaveCount(3);
    }

    #endregion

    #region GenerateRefreshToken

    [Fact]
    public void GenerateRefreshToken_ShouldReturnToken()
    {
        // Act
        var (token, expiresAt) = _generator.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldSetCorrectExpiration()
    {
        // Arrange
        var expectedExpiry = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);

        // Act
        var (_, expiresAt) = _generator.GenerateRefreshToken();

        // Assert
        expiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueTokens()
    {
        // Act
        var (token1, _) = _generator.GenerateRefreshToken();
        var (token2, _) = _generator.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldBeUrlSafe()
    {
        // Act
        var (token, _) = _generator.GenerateRefreshToken();

        // Assert - Should not contain URL-unsafe characters
        token.Should().NotContain("+");
        token.Should().NotContain("/");
        token.Should().NotContain("=");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldHaveSufficientLength()
    {
        // Act
        var (token, _) = _generator.GenerateRefreshToken();

        // Assert - 64 bytes base64 encoded (minus padding) should be at least 80+ chars
        token.Length.Should().BeGreaterOrEqualTo(80);
    }

    #endregion
}
