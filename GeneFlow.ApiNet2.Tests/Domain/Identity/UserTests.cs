using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;

namespace GeneFlow.ApiNet2.Tests.Domain.Identity;

/// <summary>
/// Unit tests for the User aggregate root.
/// </summary>
public class UserTests
{
    #region Helper Methods

    private static User CreateTestUser(long id = 1)
    {
        var userId = new UserId(id);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hashedpassword").Value;

        return User.Create(userId, email, username, passwordHash).Value;
    }

    #endregion

    #region Create

    [Fact]
    public void Create_WithValidData_ShouldReturnUser()
    {
        // Arrange
        var userId = new UserId(1);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hash").Value;

        // Act
        var result = User.Create(userId, email, username, passwordHash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be(email);
        result.Value.Username.Should().Be(username);
        result.Value.IsActive.Should().BeTrue();
        result.Value.EmailVerified.Should().BeFalse();
        result.Value.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldAssignUserRole()
    {
        // Act
        var user = CreateTestUser();

        // Assert
        user.Roles.Should().Contain(Role.User);
        user.HasRole(Role.User).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateEmailVerificationToken()
    {
        // Act
        var user = CreateTestUser();

        // Assert
        user.EmailVerificationToken.Should().NotBeNullOrEmpty();
        user.EmailVerificationTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Create_ShouldRaiseUserRegisteredEvent()
    {
        // Arrange
        var userId = new UserId(1);
        var email = Email.Create("test@example.com").Value;
        var username = Username.Create("testuser").Value;
        var passwordHash = PasswordHash.Create("$2a$12$hash").Value;

        // Act
        var result = User.Create(userId, email, username, passwordHash);

        // Assert
        result.Value.DomainEvents.Should().ContainSingle(e => e is UserRegisteredEvent);
    }

    #endregion

    #region CreateFromOAuth

    [Fact]
    public void CreateFromOAuth_ShouldCreateUserWithVerifiedEmail()
    {
        // Arrange
        var userId = new UserId(1);
        var email = Email.Create("oauth@example.com").Value;
        var username = Username.Create("oauthuser").Value;

        // Act
        var result = User.CreateFromOAuth(userId, email, username, ExternalProvider.Google, "google_key_123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EmailVerified.Should().BeTrue();
        result.Value.HasPassword.Should().BeFalse();
        result.Value.ExternalLogins.Should().HaveCount(1);
    }

    #endregion

    #region VerifyEmail

    [Fact]
    public void VerifyEmail_WithValidToken_ShouldVerifyEmail()
    {
        // Arrange
        var user = CreateTestUser();
        var token = user.EmailVerificationToken!;

        // Act
        var result = user.VerifyEmail(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public void VerifyEmail_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.VerifyEmail("invalid_token");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void VerifyEmail_ShouldRaiseUserEmailVerifiedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var token = user.EmailVerificationToken!;
        user.ClearDomainEvents();

        // Act
        user.VerifyEmail(token);

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is UserEmailVerifiedEvent);
    }

    #endregion

    #region Password Reset

    [Fact]
    public void RequestPasswordReset_ShouldGenerateToken()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = user.RequestPasswordReset();

        // Assert
        token.Should().NotBeNullOrEmpty();
        user.PasswordResetToken.Should().Be(token);
        user.PasswordResetTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void RequestPasswordReset_ShouldRaiseEvent()
    {
        // Arrange
        var user = CreateTestUser();
        user.ClearDomainEvents();

        // Act
        user.RequestPasswordReset();

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is PasswordResetRequestedEvent);
    }

    [Fact]
    public void ResetPassword_WithValidToken_ShouldChangePassword()
    {
        // Arrange
        var user = CreateTestUser();
        var token = user.RequestPasswordReset();
        var newPasswordHash = PasswordHash.Create("$2a$12$newpasswordhash").Value;

        // Act
        var result = user.ResetPassword(token, newPasswordHash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be(newPasswordHash);
        user.PasswordResetToken.Should().BeNull();
    }

    [Fact]
    public void ResetPassword_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.RequestPasswordReset();
        var newPasswordHash = PasswordHash.Create("$2a$12$newpasswordhash").Value;

        // Act
        var result = user.ResetPassword("invalid_token", newPasswordHash);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Two-Factor Authentication

    [Fact]
    public void EnableTwoFactor_ShouldEnableTwoFactor()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.EnableTwoFactor();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public void EnableTwoFactor_WhenAlreadyEnabled_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.EnableTwoFactor();

        // Act
        var result = user.EnableTwoFactor();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DisableTwoFactor_ShouldDisableTwoFactor()
    {
        // Arrange
        var user = CreateTestUser();
        user.EnableTwoFactor();

        // Act
        var result = user.DisableTwoFactor();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public void DisableTwoFactor_WhenNotEnabled_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.DisableTwoFactor();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void GenerateTwoFactorCode_ShouldGenerateCode()
    {
        // Arrange
        var user = CreateTestUser();
        user.EnableTwoFactor();

        // Act
        var code = user.GenerateTwoFactorCode();

        // Assert
        code.Should().NotBeNull();
        code.Code.Should().HaveLength(6);
    }

    #endregion

    #region Login Attempts & Lockout

    [Fact]
    public void RecordFailedLogin_ShouldIncrementFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.RecordFailedLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public void RecordFailedLogin_WhenReachingMax_ShouldLockout()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        for (var i = 0; i < AccountLockout.MaxFailedAttempts; i++)
        {
            user.RecordFailedLogin();
        }

        // Assert
        user.IsLockedOut.Should().BeTrue();
        user.LockoutEnd.Should().NotBeNull();
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldResetLockout()
    {
        // Arrange
        var user = CreateTestUser();
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.IsLockedOut.Should().BeFalse();
    }

    #endregion

    #region Refresh Tokens

    [Fact]
    public void AddRefreshToken_ShouldAddToken()
    {
        // Arrange
        var user = CreateTestUser();
        var token = RefreshToken.Create("token123", DateTime.UtcNow.AddDays(7)).Value;

        // Act
        user.AddRefreshToken(token);

        // Assert
        user.RefreshTokens.Should().Contain(token);
    }

    [Fact]
    public void RevokeRefreshToken_WithValidToken_ShouldRevoke()
    {
        // Arrange
        var user = CreateTestUser();
        var token = RefreshToken.Create("token123", DateTime.UtcNow.AddDays(7)).Value;
        user.AddRefreshToken(token);

        // Act
        var result = user.RevokeRefreshToken("token123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.GetRefreshToken("token123")!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void RevokeRefreshToken_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.RevokeRefreshToken("nonexistent");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RevokeAllRefreshTokens_ShouldRevokeAll()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRefreshToken(RefreshToken.Create("token1", DateTime.UtcNow.AddDays(7)).Value);
        user.AddRefreshToken(RefreshToken.Create("token2", DateTime.UtcNow.AddDays(7)).Value);

        // Act
        user.RevokeAllRefreshTokens();

        // Assert
        user.RefreshTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    #endregion

    #region Roles

    [Fact]
    public void AddRole_ShouldAddRole()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.AddRole(Role.Admin);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.HasRole(Role.Admin).Should().BeTrue();
    }

    [Fact]
    public void AddRole_WhenAlreadyHasRole_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRole(Role.Admin);

        // Act
        var result = user.AddRole(Role.Admin);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RemoveRole_ShouldRemoveRole()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRole(Role.Admin);

        // Act
        var result = user.RemoveRole(Role.Admin);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.HasRole(Role.Admin).Should().BeFalse();
    }

    [Fact]
    public void RemoveRole_WhenNotHasRole_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.RemoveRole(Role.Admin);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region External Logins

    [Fact]
    public void LinkExternalLogin_ShouldAddExternalLogin()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.LinkExternalLogin(ExternalProvider.Google, "google_key_123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ExternalLogins.Should().HaveCount(1);
    }

    [Fact]
    public void LinkExternalLogin_WhenAlreadyLinked_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.LinkExternalLogin(ExternalProvider.Google, "google_key_123");

        // Act
        var result = user.LinkExternalLogin(ExternalProvider.Google, "google_key_123");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UnlinkExternalLogin_ShouldRemoveExternalLogin()
    {
        // Arrange
        var user = CreateTestUser();
        user.LinkExternalLogin(ExternalProvider.Google, "google_key_123");

        // Act
        var result = user.UnlinkExternalLogin(ExternalProvider.Google);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.ExternalLogins.Should().BeEmpty();
    }

    #endregion

    #region Deactivate/Activate

    [Fact]
    public void Deactivate_ShouldDeactivateUser()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRefreshToken(RefreshToken.Create("token", DateTime.UtcNow.AddDays(7)).Value);

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.RefreshTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public void Activate_ShouldActivateUser()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldNotChangeState()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate();
        user.ClearDomainEvents();

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotChangeState()
    {
        // Arrange
        var user = CreateTestUser();
        user.ClearDomainEvents();

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldRaiseUserDeactivatedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        user.ClearDomainEvents();

        // Act
        user.Deactivate();

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is UserDeactivatedEvent);
    }

    #endregion

    #region ChangePassword

    [Fact]
    public void ChangePassword_ShouldUpdatePasswordHash()
    {
        // Arrange
        var user = CreateTestUser();
        var newPasswordHash = PasswordHash.Create("$2a$12$newpasswordhash").Value;

        // Act
        user.ChangePassword(newPasswordHash);

        // Assert
        user.PasswordHash.Should().Be(newPasswordHash);
    }

    [Fact]
    public void ChangePassword_ShouldRaiseUserPasswordChangedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        user.ClearDomainEvents();
        var newPasswordHash = PasswordHash.Create("$2a$12$newpasswordhash").Value;

        // Act
        user.ChangePassword(newPasswordHash);

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is UserPasswordChangedEvent);
    }

    #endregion

    #region RegenerateEmailVerificationToken

    [Fact]
    public void RegenerateEmailVerificationToken_ShouldReturnNewToken()
    {
        // Arrange
        var user = CreateTestUser();
        var originalToken = user.EmailVerificationToken;

        // Act
        var newToken = user.RegenerateEmailVerificationToken();

        // Assert
        newToken.Should().NotBeNullOrEmpty();
        newToken.Should().NotBe(originalToken);
    }

    [Fact]
    public void RegenerateEmailVerificationToken_ShouldUpdateTokenExpiry()
    {
        // Arrange
        var user = CreateTestUser();
        var originalExpiry = user.EmailVerificationTokenExpiry;

        // Act
        user.RegenerateEmailVerificationToken();

        // Assert
        user.EmailVerificationTokenExpiry.Should().BeAfter(DateTime.UtcNow);
    }

    #endregion

    #region VerifyEmail - Additional Cases

    [Fact]
    public void VerifyEmail_WhenAlreadyVerified_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        var token = user.EmailVerificationToken!;
        user.VerifyEmail(token);
        user.ClearDomainEvents();

        // Act - try to verify again (idempotent operation)
        var result = user.VerifyEmail("any_token");

        // Assert - idempotent: already verified returns success
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Password Reset - Additional Cases

    [Fact]
    public void ResetPassword_ShouldRaiseUserPasswordChangedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var token = user.RequestPasswordReset();
        var newPasswordHash = PasswordHash.Create("$2a$12$newpasswordhash").Value;
        user.ClearDomainEvents();

        // Act
        user.ResetPassword(token, newPasswordHash);

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is UserPasswordChangedEvent);
    }

    [Fact]
    public void ResetPassword_ShouldClearPasswordResetToken()
    {
        // Arrange
        var user = CreateTestUser();
        var token = user.RequestPasswordReset();
        var newPasswordHash = PasswordHash.Create("$2a$12$newpasswordhash").Value;

        // Act
        user.ResetPassword(token, newPasswordHash);

        // Assert
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
    }

    #endregion

    #region ValidateTwoFactorCode

    [Fact]
    public void ValidateTwoFactorCode_WithValidCode_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUser();
        user.EnableTwoFactor();
        var code = user.GenerateTwoFactorCode();

        // Act
        var result = user.ValidateTwoFactorCode(code.Code);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateTwoFactorCode_WithInvalidCode_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();
        user.EnableTwoFactor();
        user.GenerateTwoFactorCode();

        // Act
        var result = user.ValidateTwoFactorCode("invalid");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Login Attempts - Additional Cases

    [Fact]
    public void RecordFailedLogin_ShouldRaiseUserLockedOutEventWhenLocked()
    {
        // Arrange
        var user = CreateTestUser();
        user.ClearDomainEvents();

        // Act - reach max failed attempts
        for (var i = 0; i < AccountLockout.MaxFailedAttempts; i++)
        {
            user.RecordFailedLogin();
        }

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is UserLockedOutEvent);
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldClearLockoutEnd()
    {
        // Arrange
        var user = CreateTestUser();
        for (var i = 0; i < AccountLockout.MaxFailedAttempts; i++)
        {
            user.RecordFailedLogin();
        }

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.LockoutEnd.Should().BeNull();
    }

    #endregion

    #region Refresh Tokens - Additional Cases

    [Fact]
    public void AddRefreshToken_ShouldRemoveInactiveTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var revokedToken = RefreshToken.Create("revoked", DateTime.UtcNow.AddDays(7)).Value.Revoke();
        user.AddRefreshToken(revokedToken);

        var activeToken = RefreshToken.Create("active", DateTime.UtcNow.AddDays(7)).Value;

        // Act
        user.AddRefreshToken(activeToken);

        // Assert - revoked (inactive) token should be removed
        user.RefreshTokens.Should().NotContain(t => t.Token == "revoked");
        user.RefreshTokens.Should().Contain(t => t.Token == "active");
    }

    [Fact]
    public void GetRefreshToken_WithNonexistentToken_ShouldReturnNull()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var token = user.GetRefreshToken("nonexistent");

        // Assert
        token.Should().BeNull();
    }

    #endregion

    #region External Logins - Additional Cases

    [Fact]
    public void LinkExternalLogin_ShouldRaiseExternalLoginLinkedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        user.ClearDomainEvents();

        // Act
        user.LinkExternalLogin(ExternalProvider.Google, "google_key_123");

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is ExternalLoginLinkedEvent);
    }

    [Fact]
    public void UnlinkExternalLogin_WhenNotLinked_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.UnlinkExternalLogin(ExternalProvider.Google);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void LinkExternalLogin_WithMultipleProviders_ShouldSupportAll()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.LinkExternalLogin(ExternalProvider.Google, "google_key");
        user.LinkExternalLogin(ExternalProvider.GitHub, "github_key");

        // Assert
        user.ExternalLogins.Should().HaveCount(2);
    }

    #endregion

    #region Roles - Additional Cases

    [Fact]
    public void AddRole_ShouldRaiseUserRoleAddedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        user.ClearDomainEvents();

        // Act
        user.AddRole(Role.Admin);

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is UserRoleAddedEvent);
    }

    [Fact]
    public void RemoveRole_ShouldRaiseUserRoleRemovedEvent()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRole(Role.Admin);
        user.ClearDomainEvents();

        // Act
        user.RemoveRole(Role.Admin);

        // Assert
        user.DomainEvents.Should().ContainSingle(e => e is UserRoleRemovedEvent);
    }

    #endregion

    #region SoftDelete

    [Fact]
    public void SoftDelete_ShouldDeactivateUser()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.SoftDelete();

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_ShouldRevokeAllRefreshTokens()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddRefreshToken(RefreshToken.Create("token1", DateTime.UtcNow.AddDays(7)).Value);
        user.AddRefreshToken(RefreshToken.Create("token2", DateTime.UtcNow.AddDays(7)).Value);

        // Act
        user.SoftDelete();

        // Assert
        user.RefreshTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldNotChangeState()
    {
        // Arrange
        var user = CreateTestUser();
        user.SoftDelete();
        var deletedAt = user.DeletedAt;

        // Act
        user.SoftDelete();

        // Assert
        user.DeletedAt.Should().Be(deletedAt);
    }

    #endregion

    #region HasPassword

    [Fact]
    public void HasPassword_WhenCreatedWithPassword_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser();

        // Assert
        user.HasPassword.Should().BeTrue();
    }

    [Fact]
    public void HasPassword_WhenCreatedFromOAuth_ShouldReturnFalse()
    {
        // Arrange
        var userId = new UserId(1);
        var email = Email.Create("oauth@example.com").Value;
        var username = Username.Create("oauthuser").Value;

        // Act
        var result = User.CreateFromOAuth(userId, email, username, ExternalProvider.Google, "google_key_123");

        // Assert
        result.Value.HasPassword.Should().BeFalse();
    }

    #endregion
}
