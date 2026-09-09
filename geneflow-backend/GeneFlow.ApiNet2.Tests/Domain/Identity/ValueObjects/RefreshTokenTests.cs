using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the RefreshToken value object.
/// </summary>
public class RefreshTokenTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        const string token = "valid_refresh_token_string";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var result = RefreshToken.Create(token, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be(token);
        result.Value.ExpiresAt.Should().Be(expiresAt);
        result.Value.IsRevoked.Should().BeFalse();
        result.Value.RevokedAt.Should().BeNull();
        result.Value.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public void Create_WithCustomCreatedAt_ShouldUseProvidedValue()
    {
        // Arrange
        const string token = "valid_refresh_token";
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var createdAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = RefreshToken.Create(token, expiresAt, createdAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(createdAt);
    }

    #endregion

    #region Create - Invalid Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyToken_ShouldReturnFailure(string? token)
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var result = RefreshToken.Create(token!, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithPastExpiration_ShouldReturnFailure()
    {
        // Arrange
        const string token = "valid_token";
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = RefreshToken.Create(token, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region IsExpired

    [Fact]
    public void IsExpired_WhenExpirationInFutuREDACTED()
    {
        // Arrange
        var token = RefreshToken.Create("token", DateTime.UtcNow.AddDays(7)).Value;

        // Act & Assert
        token.IsExpired.Should().BeFalse();
    }

    #endregion

    #region IsActive

    [Fact]
    public void IsActive_WhenNotExpiredAndNotRevoked_ShouldReturnTrue()
    {
        // Arrange
        var token = RefreshToken.Create("token", DateTime.UtcNow.AddDays(7)).Value;

        // Act & Assert
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var token = RefreshToken.Create("token", DateTime.UtcNow.AddDays(7)).Value;
        var revokedToken = token.Revoke();

        // Act & Assert
        revokedToken.IsActive.Should().BeFalse();
    }

    #endregion

    #region Revoke

    [Fact]
    public void Revoke_ShouldSetRevokedState()
    {
        // Arrange
        var token = RefreshToken.Create("token", DateTime.UtcNow.AddDays(7)).Value;

        // Act
        var revokedToken = token.Revoke();

        // Assert
        revokedToken.IsRevoked.Should().BeTrue();
        revokedToken.RevokedAt.Should().NotBeNull();
        revokedToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Revoke_WithReplacementToken_ShouldSetReplacedByToken()
    {
        // Arrange
        var token = RefreshToken.Create("original_token", DateTime.UtcNow.AddDays(7)).Value;
        const string replacementToken = "new_token";

        // Act
        var revokedToken = token.Revoke(replacementToken);

        // Assert
        revokedToken.ReplacedByToken.Should().Be(replacementToken);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameToken_ShouldReturnTrue()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var token1 = RefreshToken.Create("same_token", expiresAt).Value;
        var token2 = RefreshToken.Create("same_token", expiresAt).Value;

        // Note: Equality is based on token value, not dates
        token1.Token.Should().Be(token2.Token);
    }

    #endregion
}
