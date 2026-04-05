using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Services;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Application.Identity.Services;

/// <summary>
/// Unit tests for UserAuthenticationValidator.
/// </summary>
public class UserAuthenticationValidatorTests
{
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly UserAuthenticationValidator _validator;

    public UserAuthenticationValidatorTests()
    {
        _validator = new UserAuthenticationValidator(_passwordHasher);
    }

    #region Helper Methods

    private static User CreateActiveVerifiedUser()
    {
        var userId = new UserId(1);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        var user = User.Create(userId, email, username, passwordHash).Value;
        user.VerifyEmail(user.EmailVerificationToken!);
        return user;
    }

    private static User CreateUnverifiedUser()
    {
        var userId = new UserId(1);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        return User.Create(userId, email, username, passwordHash).Value;
    }

    #endregion

    #region ValidateCanAuthenticate

    [Fact]
    public void ValidateCanAuthenticate_WithActiveVerifiedUser_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateActiveVerifiedUser();

        // Act
        var result = _validator.ValidateCanAuthenticate(user);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateCanAuthenticate_WithDeactivatedUser_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateActiveVerifiedUser();
        user.Deactivate();

        // Act
        var result = _validator.ValidateCanAuthenticate(user);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("UserDeactivated");
    }

    [Fact]
    public void ValidateCanAuthenticate_WithLockedOutUser_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateActiveVerifiedUser();

        // Lock out the user
        for (var i = 0; i < AccountLockout.MaxFailedAttempts; i++)
        {
            user.RecordFailedLogin();
        }

        // Act
        var result = _validator.ValidateCanAuthenticate(user);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("AccountLocked");
    }

    [Fact]
    public void ValidateCanAuthenticate_WithUnverifiedEmail_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateUnverifiedUser();

        // Act
        var result = _validator.ValidateCanAuthenticate(user);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EmailNotVerified");
    }

    #endregion

    #region ValidatePassword

    [Fact]
    public void ValidatePassword_WithCorrectPassword_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateActiveVerifiedUser();
        const string password = "Password123!";

        _passwordHasher
            .Verify(password, user.PasswordHash.Value)
            .Returns(true);

        // Act
        var result = _validator.ValidatePassword(user, password);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidatePassword_WithIncorrectPassword_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateActiveVerifiedUser();
        const string password = "WrongPassword!";

        _passwordHasher
            .Verify(password, user.PasswordHash.Value)
            .Returns(false);

        // Act
        var result = _validator.ValidatePassword(user, password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCredentials");
    }

    [Fact]
    public void ValidatePassword_WithOAuthUser_ShouldReturnFailure()
    {
        // Arrange - OAuth users don't have a password
        var userId = new UserId(1);
        var email = Email.Create("oauth@example.com").Value;
        var username = Username.Create("oauthuser").Value;
        var user = User.CreateFromOAuth(userId, email, username, GeneFlow.ApiNet2.Domain.Identity.Enumerations.ExternalProvider.Google, "google_key").Value;

        // Act
        var result = _validator.ValidatePassword(user, "anypassword");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCredentials");
    }

    #endregion
}
