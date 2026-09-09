using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the PasswordReset value object.
/// </summary>
public class PasswordResetTests
{
    #region None

    [Fact]
    public void None_ShouldReturnEmptyState()
    {
        // Act
        var reset = PasswordReset.None;

        // Assert
        reset.Token.Should().BeNull();
        reset.TokenExpiry.Should().BeNull();
        reset.IsPending.Should().BeFalse();
    }

    #endregion

    #region Request - Valid Cases

    [Fact]
    public void Request_ShouldCreateTokenWithExpiry()
    {
        // Arrange
        var beforeRequest = DateTime.UtcNow;

        // Act
        var reset = PasswordReset.Request();

        // Assert
        reset.Token.Should().NotBeNullOrWhiteSpace();
        reset.TokenExpiry.Should().NotBeNull();
        reset.TokenExpiry.Should().BeAfter(beforeRequest);
    }

    [Fact]
    public void Request_ShouldSetExpiryTo1Hour()
    {
        // Arrange
        var beforeRequest = DateTime.UtcNow;

        // Act
        var reset = PasswordReset.Request();

        // Assert
        reset.TokenExpiry.Should().BeCloseTo(
            beforeRequest.AddHours(1),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Request_ShouldGenerateUniqueTokens()
    {
        // Act
        var reset1 = PasswordReset.Request();
        var reset2 = PasswordReset.Request();

        // Assert
        reset1.Token.Should().NotBe(reset2.Token);
    }

    #endregion

    #region IsPending

    [Fact]
    public void IsPending_WhenTokenExistsAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var reset = PasswordReset.Request();

        // Assert
        reset.IsPending.Should().BeTrue();
    }

    [Fact]
    public void IsPending_WhenNone_ShouldReturnFalse()
    {
        // Arrange
        var reset = PasswordReset.None;

        // Assert
        reset.IsPending.Should().BeFalse();
    }

    #endregion

    #region Validate - Valid Cases

    [Fact]
    public void Validate_WithCorrectToken_ShouldReturnSuccess()
    {
        // Arrange
        var reset = PasswordReset.Request();
        var token = reset.Token!;

        // Act
        var result = reset.Validate(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Validate - Invalid Cases

    [Fact]
    public void Validate_WithWrongToken_ShouldReturnFailure()
    {
        // Arrange
        var reset = PasswordReset.Request();

        // Act
        var result = reset.Validate("wrong_token");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidPasswordResetToken");
    }

    [Fact]
    public void Validate_WithEmptyToken_ShouldReturnFailure()
    {
        // Arrange
        var reset = PasswordReset.Request();

        // Act
        var result = reset.Validate(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidPasswordResetToken");
    }

    [Fact]
    public void Validate_WhenNoTokenSet_ShouldReturnFailure()
    {
        // Arrange
        var reset = PasswordReset.None;

        // Act
        var result = reset.Validate("any_token");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidPasswordResetToken");
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_ShouldReturnNoneState()
    {
        // Arrange
        var reset = PasswordReset.Request();

        // Act
        var cleared = reset.Clear();

        // Assert
        cleared.Token.Should().BeNull();
        cleared.TokenExpiry.Should().BeNull();
        cleared.IsPending.Should().BeFalse();
    }

    [Fact]
    public void Clear_WhenAlreadyNone_ShouldReturnNone()
    {
        // Arrange
        var reset = PasswordReset.None;

        // Act
        var cleared = reset.Clear();

        // Assert
        cleared.Should().Be(PasswordReset.None);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_TwoNoneInstances_ShouldBeEqual()
    {
        // Arrange
        var none1 = PasswordReset.None;
        var none2 = PasswordReset.None;

        // Assert
        none1.Should().Be(none2);
    }

    [Fact]
    public void Equals_DifferentTokens_ShouldNotBeEqual()
    {
        // Arrange
        var reset1 = PasswordReset.Request();
        var reset2 = PasswordReset.Request();

        // Assert
        reset1.Should().NotBe(reset2);
    }

    [Fact]
    public void Equals_NoneAndRequest_ShouldNotBeEqual()
    {
        // Arrange
        var none = PasswordReset.None;
        var request = PasswordReset.Request();

        // Assert
        none.Should().NotBe(request);
    }

    #endregion
}
