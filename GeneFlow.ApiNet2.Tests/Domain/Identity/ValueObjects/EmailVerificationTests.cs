using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the EmailVerification value object.
/// </summary>
public class EmailVerificationTests
{
    #region CreatePending - Valid Cases

    [Fact]
    public void CreatePending_ShouldReturnPendingVerification()
    {
        // Act
        var verification = EmailVerification.CreatePending();

        // Assert
        verification.IsVerified.Should().BeFalse();
        verification.Token.Should().NotBeNullOrWhiteSpace();
        verification.TokenExpiry.Should().NotBeNull();
        verification.TokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void CreatePending_ShouldGenerateTokenExpiringIn24Hours()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var verification = EmailVerification.CreatePending();

        // Assert
        verification.TokenExpiry.Should().BeCloseTo(
            beforeCreation.AddHours(24),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreatePending_ShouldGenerateUniqueTokens()
    {
        // Act
        var verification1 = EmailVerification.CreatePending();
        var verification2 = EmailVerification.CreatePending();

        // Assert
        verification1.Token.Should().NotBe(verification2.Token);
    }

    #endregion

    #region CreateVerified

    [Fact]
    public void CreateVerified_ShouldReturnVerifiedState()
    {
        // Act
        var verification = EmailVerification.CreateVerified();

        // Assert
        verification.IsVerified.Should().BeTrue();
        verification.Token.Should().BeNull();
        verification.TokenExpiry.Should().BeNull();
    }

    #endregion

    #region Verify - Valid Cases

    [Fact]
    public void Verify_WithCorrectToken_ShouldReturnVerifiedState()
    {
        // Arrange
        var pending = EmailVerification.CreatePending();
        var token = pending.Token!;

        // Act
        var result = pending.Verify(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsVerified.Should().BeTrue();
        result.Value.Token.Should().BeNull();
        result.Value.TokenExpiry.Should().BeNull();
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_ShouldReturnSameInstance()
    {
        // Arrange
        var verified = EmailVerification.CreateVerified();

        // Act
        var result = verified.Verify("any_token");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(verified);
    }

    #endregion

    #region Verify - Invalid Cases

    [Fact]
    public void Verify_WithWrongToken_ShouldReturnFailure()
    {
        // Arrange
        var pending = EmailVerification.CreatePending();

        // Act
        var result = pending.Verify("wrong_token");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidVerificationToken");
    }

    [Fact]
    public void Verify_WithEmptyToken_ShouldReturnFailure()
    {
        // Arrange
        var pending = EmailVerification.CreatePending();

        // Act
        var result = pending.Verify(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidVerificationToken");
    }

    #endregion

    #region RegenerateToken

    [Fact]
    public void RegenerateToken_WhenPending_ShouldCreateNewToken()
    {
        // Arrange
        var original = EmailVerification.CreatePending();
        var originalToken = original.Token;

        // Act
        var regenerated = original.RegenerateToken();

        // Assert
        regenerated.Token.Should().NotBe(originalToken);
        regenerated.IsVerified.Should().BeFalse();
        regenerated.TokenExpiry.Should().NotBeNull();
    }

    [Fact]
    public void RegenerateToken_WhenVerified_ShouldReturnSameInstance()
    {
        // Arrange
        var verified = EmailVerification.CreateVerified();

        // Act
        var result = verified.RegenerateToken();

        // Assert
        result.Should().Be(verified);
        result.IsVerified.Should().BeTrue();
    }

    [Fact]
    public void RegenerateToken_ShouldResetExpiryTo24Hours()
    {
        // Arrange
        var beforeRegeneration = DateTime.UtcNow;
        var original = EmailVerification.CreatePending();

        // Act
        var regenerated = original.RegenerateToken();

        // Assert
        regenerated.TokenExpiry.Should().BeCloseTo(
            beforeRegeneration.AddHours(24),
            TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameState_ShouldReturnTrue()
    {
        // Arrange
        var verified1 = EmailVerification.CreateVerified();
        var verified2 = EmailVerification.CreateVerified();

        // Assert
        verified1.Should().Be(verified2);
    }

    [Fact]
    public void Equals_WithDifferentState_ShouldReturnFalse()
    {
        // Arrange
        var verified = EmailVerification.CreateVerified();
        var pending = EmailVerification.CreatePending();

        // Assert
        verified.Should().NotBe(pending);
    }

    [Fact]
    public void Equals_PendingWithDifferentTokens_ShouldReturnFalse()
    {
        // Arrange
        var pending1 = EmailVerification.CreatePending();
        var pending2 = EmailVerification.CreatePending();

        // Assert
        pending1.Should().NotBe(pending2);
    }

    #endregion
}
