using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the PasswordHash value object.
/// </summary>
public class PasswordHashTests
{
    #region Create - Valid Cases

    [Fact]
    public void Create_WithValidHash_ShouldReturnSuccess()
    {
        // Arrange
        const string hash = "$2a$12$validbcrypthashvalue1234567890abcdefghijklmnopqrst";

        // Act
        var result = PasswordHash.Create(hash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(hash);
    }

    #endregion

    #region Create - Invalid Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldReturnFailure(string? hash)
    {
        // Act
        var result = PasswordHash.Create(hash!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region CreatePlaceholder

    [Fact]
    public void CreatePlaceholder_ShouldReturnPlaceholderHash()
    {
        // Act
        var hash = PasswordHash.CreatePlaceholder();

        // Assert
        hash.IsPlaceholder.Should().BeTrue();
    }

    [Fact]
    public void IsPlaceholder_WithRegularHash_ShouldReturnFalse()
    {
        // Arrange
        var hash = PasswordHash.Create("$2a$12$validhash").Value;

        // Act & Assert
        hash.IsPlaceholder.Should().BeFalse();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        const string hashValue = "$2a$12$samehash";
        var hash1 = PasswordHash.Create(hashValue).Value;
        var hash2 = PasswordHash.Create(hashValue).Value;

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var hash1 = PasswordHash.Create("$2a$12$hash1").Value;
        var hash2 = PasswordHash.Create("$2a$12$hash2").Value;

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region ToString and Implicit Conversion

    [Fact]
    public void ToString_ShouldReturnMaskedValue()
    {
        // Arrange
        var hash = PasswordHash.Create("$2a$12$testhash").Value;

        // Act & Assert - ToString returns masked value for security
        hash.ToString().Should().Be("****");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnHashValue()
    {
        // Arrange
        const string hashValue = "$2a$12$testhash";
        var hash = PasswordHash.Create(hashValue).Value;

        // Act
        string result = hash;

        // Assert
        result.Should().Be(hashValue);
    }

    #endregion
}
