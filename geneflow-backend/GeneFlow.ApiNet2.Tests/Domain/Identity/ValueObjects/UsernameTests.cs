using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity.ValueObjects;

/// <summary>
/// Unit tests for the Username value object.
/// </summary>
public class UsernameTests
{
    #region Create - Valid Cases

    [Theory]
    [InlineData("usr")]
    [InlineData("user123")]
    [InlineData("johndoe")]
    [InlineData("username123")]
    public void Create_WithValidUsername_ShouldReturnSuccess(string username)
    {
        // Act
        var result = Username.Create(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(username);
    }

    [Fact]
    public void Create_WithMixedCaseUsername_ShouldNormalizeToLowercase()
    {
        // Arrange
        const string mixedCase = "JohnDoe";

        // Act
        var result = Username.Create(mixedCase);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("johndoe");
    }

    [Fact]
    public void Create_WithMinLengthUsername_ShouldReturnSuccess()
    {
        // Arrange
        var username = new string('a', Username.MinLength);

        // Act
        var result = Username.Create(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMaxLengthUsername_ShouldReturnSuccess()
    {
        // Arrange
        var username = new string('a', Username.MaxLength);

        // Act
        var result = Username.Create(username);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Create - Invalid Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldReturnFailure(string? username)
    {
        // Act
        var result = Username.Create(username);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithTooShortUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = new string('a', Username.MinLength - 1);

        // Act
        var result = Username.Create(username);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithTooLongUsername_ShouldReturnFailure()
    {
        // Arrange
        var username = new string('a', Username.MaxLength + 1);

        // Act
        var result = Username.Create(username);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("user@name")]
    [InlineData("user name")]
    [InlineData("user.name")]
    [InlineData("user-name")]
    [InlineData("user!name")]
    [InlineData("user_name")]  // Underscores not allowed
    public void Create_WithInvalidCharacters_ShouldReturnFailure(string username)
    {
        // Act
        var result = Username.Create(username);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var username1 = Username.Create("testuser").Value;
        var username2 = Username.Create("testuser").Value;

        // Assert
        username1.Should().Be(username2);
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var username1 = Username.Create("user1").Value;
        var username2 = Username.Create("user2").Value;

        // Assert
        username1.Should().NotBe(username2);
    }

    #endregion

    #region ToString and Implicit Conversion

    [Fact]
    public void ToString_ShouldReturnUsernameValue()
    {
        // Arrange
        const string usernameValue = "testuser";
        var username = Username.Create(usernameValue).Value;

        // Act & Assert
        username.ToString().Should().Be(usernameValue);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnUsernameValue()
    {
        // Arrange
        const string usernameValue = "testuser";
        var username = Username.Create(usernameValue).Value;

        // Act
        string result = username;

        // Assert
        result.Should().Be(usernameValue);
    }

    #endregion
}
