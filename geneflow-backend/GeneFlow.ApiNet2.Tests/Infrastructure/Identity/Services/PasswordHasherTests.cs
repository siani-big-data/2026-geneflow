using GeneFlow.ApiNet2.Infrastructure.Identity.Services;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Identity.Services;

/// <summary>
/// Unit tests for the PasswordHasher service.
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    #region Hash

    [Fact]
    public void Hash_ShouldReturnBcryptHash()
    {
        // Arrange
        const string password = "Password123!";

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2"); // BCrypt prefix
    }

    [Fact]
    public void Hash_ShouldReturnDifferentHashesForSamePassword()
    {
        // Arrange
        const string password = "Password123!";

        // Act
        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        // Assert - BCrypt uses salt, so hashes should differ
        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("LongerPassword123!@#")]
    [InlineData("unicode-пароль-密码")]
    public void Hash_ShouldHandleVariousPasswords(string password)
    {
        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Verify

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "Password123!";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "Password123!";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify("WrongPassword!", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithCaseMismatch_ShouldReturnFalse()
    {
        // Arrange
        const string password = "Password123!";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify("PASSWORD123!", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldBeCaseSensitive()
    {
        // Arrange
        const string password = "Password";
        var hash = _hasher.Hash(password);

        // Act & Assert
        _hasher.Verify("Password", hash).Should().BeTrue();
        _hasher.Verify("password", hash).Should().BeFalse();
        _hasher.Verify("PASSWORD", hash).Should().BeFalse();
    }

    #endregion

    #region Round Trip

    [Theory]
    [InlineData("SimplePassword")]
    [InlineData("Complex!@#$%^&*()Password123")]
    [InlineData("   spaced password   ")]
    public void HashAndVerify_ShouldRoundTrip(string password)
    {
        // Act
        var hash = _hasher.Hash(password);
        var result = _hasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
