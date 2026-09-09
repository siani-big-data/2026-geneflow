using GeneFlow.ApiNet2.Domain.Identity.Validators;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.Validators;

/// <summary>
/// Unit tests for the RefreshTokenValidator class.
/// </summary>
public class RefreshTokenValidatorTests
{
    private readonly RefreshTokenValidator _validator = new();

    #region Valid Token Tests

    [Fact]
    public void Validate_WithValidTokenAndFutureExpiry_ShouldReturnSuccess()
    {
        // Arrange
        const string token = "valid_refresh_token";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var result = _validator.Validate(token, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("simple_token")]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")]
    [InlineData("a")]
    [InlineData("token-with-dashes_and_underscores")]
    public void Validate_WithVariousValidTokens_ShouldReturnSuccess(string token)
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(1);

        // Act
        var result = _validator.Validate(token, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithExpiryJustInFutuREDACTED()
    {
        // Arrange
        const string token = "valid_token";
        var expiresAt = DateTime.UtcNow.AddSeconds(1);

        // Act
        var result = _validator.Validate(token, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Null and Empty Token Tests

    [Fact]
    public void Validate_WithNullToken_ShouldReturnFailure()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var result = _validator.Validate(null, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenRequired");
    }

    [Fact]
    public void Validate_WithEmptyToken_ShouldReturnFailure()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var result = _validator.Validate(string.Empty, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenRequired");
    }

    [Fact]
    public void Validate_WithWhitespaceToken_ShouldReturnFailure()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var result = _validator.Validate("   ", expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenRequired");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Validate_WithNullOrWhitespaceToken_ShouldReturnFailure(string? token)
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var result = _validator.Validate(token, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenRequired");
    }

    #endregion

    #region Expiration Validation Tests

    [Fact]
    public void Validate_WithPastExpiry_ShouldReturnFailure()
    {
        // Arrange
        const string token = "valid_token";
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = _validator.Validate(token, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenExpiryInPast");
    }

    [Fact]
    public void Validate_WithExpiryAtCurrentTime_ShouldReturnFailure()
    {
        // Arrange
        const string token = "valid_token";
        var expiresAt = DateTime.UtcNow;

        // Act
        var result = _validator.Validate(token, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenExpiryInPast");
    }

    [Fact]
    public void Validate_WithVeryOldExpiry_ShouldReturnFailure()
    {
        // Arrange
        const string token = "valid_token";
        var expiresAt = DateTime.UtcNow.AddYears(-1);

        // Act
        var result = _validator.Validate(token, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenExpiryInPast");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithBothInvalidTokenAndExpiry_ShouldReturnTokenError()
    {
        // Arrange - Both token is empty and expiry is in past
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = _validator.Validate(string.Empty, expiresAt);

        // Assert
        // Token validation should fail first
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenRequired");
    }

    [Fact]
    public void Validate_WithMinDateTimeExpiry_ShouldReturnFailure()
    {
        // Arrange
        const string token = "valid_token";
        var expiresAt = DateTime.MinValue;

        // Act
        var result = _validator.Validate(token, expiresAt);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenExpiryInPast");
    }

    #endregion
}
