using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.Events;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Identity;

/// <summary>
/// User aggregate root.
/// </summary>
public sealed class User : FullAuditableAggregateRoot<UserId>
{
    /// <summary>Gets the user's email address.</summary>
    public Email Email { get; private set; } = null!;

    /// <summary>Gets the user's username.</summary>
    public Username Username { get; private set; } = null!;

    /// <summary>Gets the user's password hash.</summary>
    public PasswordHash PasswordHash { get; private set; } = null!;

    /// <summary>Gets whether the user account is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the email verification state.</summary>
    public EmailVerification EmailVerification { get; private set; } = null!;

    /// <summary>Gets the password reset state.</summary>
    public PasswordReset PasswordReset { get; private set; } = null!;

    /// <summary>Gets the account lockout state.</summary>
    public AccountLockout Lockout { get; private set; } = null!;

    /// <summary>Gets the two-factor authentication state.</summary>
    public TwoFactorAuth TwoFactorAuth { get; private set; } = null!;

    private readonly List<Role> _roles = [];

    /// <summary>Gets the user's roles.</summary>
    public IReadOnlyList<Role> Roles => _roles.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = [];

    /// <summary>Gets the user's refresh tokens.</summary>
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<ExternalLogin> _externalLogins = [];

    /// <summary>Gets the user's linked external logins.</summary>
    public IReadOnlyList<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    /// <summary>Gets whether the user's email is verified.</summary>
    public bool EmailVerified => EmailVerification.IsVerified;

    /// <summary>Gets the current email verification token, if any.</summary>
    public string? EmailVerificationToken => EmailVerification.Token;

    /// <summary>Gets when the email verification token expires.</summary>
    public DateTime? EmailVerificationTokenExpiry => EmailVerification.TokenExpiry;

    /// <summary>Gets whether the user has a password set.</summary>
    public bool HasPassword => !PasswordHash.IsPlaceholder;

    /// <summary>Gets the current password reset token, if any.</summary>
    public string? PasswordResetToken => PasswordReset.Token;

    /// <summary>Gets when the password reset token expires.</summary>
    public DateTime? PasswordResetTokenExpiry => PasswordReset.TokenExpiry;

    /// <summary>Gets whether two-factor authentication is enabled.</summary>
    public bool TwoFactorEnabled => TwoFactorAuth.IsEnabled;

    /// <summary>Gets the active two-factor codes.</summary>
    public IReadOnlyList<TwoFactorCode> TwoFactorCodes => TwoFactorAuth.Codes;

    /// <summary>Gets the number of failed login attempts.</summary>
    public int FailedLoginAttempts => Lockout.FailedAttempts;

    /// <summary>Gets when the account lockout ends, if locked.</summary>
    public DateTime? LockoutEnd => Lockout.LockoutEnd;

    /// <summary>Gets whether the account is currently locked out.</summary>
    public bool IsLockedOut => Lockout.IsLockedOut;

    private User() : base() { }

    private User(
        UserId id,
        Email email,
        Username username,
        PasswordHash passwordHash) : base(id)
    {
        Email = email;
        Username = username;
        PasswordHash = passwordHash;
        IsActive = true;

        EmailVerification = EmailVerification.CreatePending();
        PasswordReset = PasswordReset.None;
        Lockout = AccountLockout.None;
        TwoFactorAuth = TwoFactorAuth.Disabled;

        InitializeCreatedAt();
        _roles.Add(Role.User);
    }

    /// <summary>
    /// Creates a new user with email/password authentication.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="username">The user's username.</param>
    /// <param name="passwordHash">The user's hashed password.</param>
    /// <returns>A result containing the created user.</returns>
    public static Result<User> Create(UserId id, Email email, Username username, PasswordHash passwordHash)
    {
        var user = new User(id, email, username, passwordHash);

        user.RaiseDomainEvent(new UserRegisteredEvent(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            user.EmailVerification.Token!));

        return user;
    }

    /// <summary>
    /// Creates a new user via OAuth provider.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="email">The user's email address from OAuth.</param>
    /// <param name="username">The user's username.</param>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="providerKey">The unique key from the provider.</param>
    /// <param name="providerDisplayName">Optional display name from provider.</param>
    /// <returns>A result containing the created user.</returns>
    public static Result<User> CreateFromOAuth(
        UserId id,
        Email email,
        Username username,
        ExternalProvider provider,
        string providerKey,
        string? providerDisplayName = null)
    {
        var user = new User(id, email, username, PasswordHash.CreatePlaceholder());

        user.EmailVerification = EmailVerification.CreateVerified();

        var externalLogin = ExternalLogin.Create(provider, providerKey, providerDisplayName);
        user._externalLogins.Add(externalLogin);

        user.RaiseDomainEvent(new UserRegisteredViaOAuthEvent(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            provider,
            providerKey));

        return user;
    }

    /// <summary>
    /// Verifies the user's email with the provided token.
    /// </summary>
    /// <param name="token">The verification token.</param>
    /// <returns>Success if verified; failure otherwise.</returns>
    public Result VerifyEmail(string token)
    {
        var result = EmailVerification.Verify(token);

        if (result.IsFailure)
            return Result.Failure(result.Error);

        EmailVerification = result.Value;
        SetModified();

        RaiseDomainEvent(new UserEmailVerifiedEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Regenerates the email verification token.
    /// </summary>
    /// <returns>The new verification token.</returns>
    public string RegenerateEmailVerificationToken()
    {
        EmailVerification = EmailVerification.RegenerateToken();
        SetModified();
        return EmailVerification.Token!;
    }

    /// <summary>
    /// Requests a password reset.
    /// </summary>
    /// <returns>The password reset token.</returns>
    public string RequestPasswordReset()
    {
        PasswordReset = PasswordReset.Request();
        SetModified();

        RaiseDomainEvent(new PasswordResetRequestedEvent(
            Id,
            Email.Value,
            Username.Value,
            PasswordReset.Token!));

        return PasswordReset.Token!;
    }

    /// <summary>
    /// Resets the password using a reset token.
    /// </summary>
    /// <param name="token">The password reset token.</param>
    /// <param name="newPasswordHash">The new password hash.</param>
    /// <returns>Success if reset; failure otherwise.</returns>
    public Result ResetPassword(string token, PasswordHash newPasswordHash)
    {
        var validationResult = PasswordReset.Validate(token);

        if (validationResult.IsFailure)
            return validationResult;

        PasswordHash = newPasswordHash;
        PasswordReset = PasswordReset.Clear();
        SetModified();

        RaiseDomainEvent(new UserPasswordChangedEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Changes the user's password directly.
    /// </summary>
    /// <param name="newPasswordHash">The new password hash.</param>
    public void ChangePassword(PasswordHash newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        SetModified();

        RaiseDomainEvent(new UserPasswordChangedEvent(Id));
    }

    /// <summary>
    /// Enables two-factor authentication.
    /// </summary>
    /// <returns>Success if enabled; failure otherwise.</returns>
    public Result EnableTwoFactor()
    {
        var result = TwoFactorAuth.Enable();

        if (result.IsFailure)
            return Result.Failure(result.Error);

        TwoFactorAuth = result.Value;
        SetModified();

        RaiseDomainEvent(new UserTwoFactorEnabledEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Disables two-factor authentication.
    /// </summary>
    /// <returns>Success if disabled; failure otherwise.</returns>
    public Result DisableTwoFactor()
    {
        var result = TwoFactorAuth.Disable();

        if (result.IsFailure)
            return Result.Failure(result.Error);

        TwoFactorAuth = result.Value;
        SetModified();

        RaiseDomainEvent(new UserTwoFactorDisabledEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Generates a new two-factor authentication code.
    /// </summary>
    /// <returns>The generated two-factor code.</returns>
    public TwoFactorCode GenerateTwoFactorCode()
    {
        var (newAuth, code) = TwoFactorAuth.GenerateCode();
        TwoFactorAuth = newAuth;
        SetModified();

        RaiseDomainEvent(new TwoFactorCodeGeneratedEvent(
            Id,
            Email.Value,
            Username.Value,
            code.Code));

        return code;
    }

    /// <summary>
    /// Validates a two-factor authentication code.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <returns>Success if valid; failure otherwise.</returns>
    public Result ValidateTwoFactorCode(string code)
    {
        var result = TwoFactorAuth.ValidateCode(code);

        if (result.IsFailure)
            return Result.Failure(result.Error);

        TwoFactorAuth = result.Value;
        SetModified();

        return Result.Success();
    }

    /// <summary>
    /// Records a failed login attempt.
    /// </summary>
    public void RecordFailedLogin()
    {
        var (newLockout, wasLockedOut) = Lockout.RecordFailedAttempt();
        Lockout = newLockout;
        SetModified();

        if (wasLockedOut)
        {
            RaiseDomainEvent(new UserLockedOutEvent(Id, Lockout.LockoutEnd!.Value, Lockout.FailedAttempts));
        }
    }

    /// <summary>
    /// Records a successful login, resetting the lockout counter.
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        Lockout = Lockout.Reset();
        SetModified();
    }

    /// <summary>
    /// Adds a refresh token to the user.
    /// </summary>
    /// <param name="token">The refresh token to add.</param>
    public void AddRefreshToken(RefreshToken token)
    {
        _refreshTokens.RemoveAll(t => !t.IsActive);
        _refreshTokens.Add(token);
        SetModified();
    }

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    /// <param name="token">The token string to revoke.</param>
    /// <param name="replacedByToken">Optional replacement token.</param>
    /// <returns>Success if revoked; failure if not found.</returns>
    public Result RevokeRefreshToken(string token, string? replacedByToken = null)
    {
        var index = _refreshTokens.FindIndex(t => t.Token == token);

        if (index < 0)
            return Result.Failure(UserErrors.RefreshTokenNotFound);

        var refreshToken = _refreshTokens[index];
        _refreshTokens[index] = refreshToken.Revoke(replacedByToken);
        SetModified();

        return Result.Success();
    }

    /// <summary>
    /// Revokes all active refresh tokens.
    /// </summary>
    public void RevokeAllRefreshTokens()
    {
        for (var i = 0; i < _refreshTokens.Count; i++)
        {
            if (_refreshTokens[i].IsActive)
            {
                _refreshTokens[i] = _refreshTokens[i].Revoke();
            }
        }
        SetModified();
    }

    /// <summary>
    /// Gets a refresh token by its value.
    /// </summary>
    /// <param name="token">The token string to find.</param>
    /// <returns>The refresh token if found; null otherwise.</returns>
    public RefreshToken? GetRefreshToken(string token)
        => _refreshTokens.FirstOrDefault(t => t.Token == token);

    /// <summary>
    /// Adds a role to the user.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <returns>Success if added; failure if already assigned.</returns>
    public Result AddRole(Role role)
    {
        if (_roles.Contains(role))
            return Result.Failure(UserErrors.RoleAlreadyAssigned);

        _roles.Add(role);
        SetModified();

        RaiseDomainEvent(new UserRoleAddedEvent(Id, role));

        return Result.Success();
    }

    /// <summary>
    /// Removes a role from the user.
    /// </summary>
    /// <param name="role">The role to remove.</param>
    /// <returns>Success if removed; failure if not assigned.</returns>
    public Result RemoveRole(Role role)
    {
        if (!_roles.Contains(role))
            return Result.Failure(UserErrors.RoleNotAssigned);

        _roles.Remove(role);
        SetModified();

        RaiseDomainEvent(new UserRoleRemovedEvent(Id, role));

        return Result.Success();
    }

    /// <summary>
    /// Checks if the user has a specific role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user has the role; false otherwise.</returns>
    public bool HasRole(Role role) => _roles.Contains(role);

    /// <summary>
    /// Links an external OAuth login to this user.
    /// </summary>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="providerKey">The provider's unique key.</param>
    /// <param name="displayName">Optional display name.</param>
    /// <returns>Success if linked; failure if already linked.</returns>
    public Result LinkExternalLogin(ExternalProvider provider, string providerKey, string? displayName = null)
    {
        if (_externalLogins.Any(e => e.Provider == provider && e.ProviderKey == providerKey))
            return Result.Failure(UserErrors.ExternalLoginAlreadyLinked);

        var externalLogin = ExternalLogin.Create(provider, providerKey, displayName);
        _externalLogins.Add(externalLogin);
        SetModified();

        RaiseDomainEvent(new ExternalLoginLinkedEvent(Id, provider, providerKey));

        return Result.Success();
    }

    /// <summary>
    /// Unlinks an external OAuth login from this user.
    /// </summary>
    /// <param name="provider">The provider to unlink.</param>
    /// <returns>Success if unlinked; failure if not found.</returns>
    public Result UnlinkExternalLogin(ExternalProvider provider)
    {
        var externalLogin = _externalLogins.FirstOrDefault(e => e.Provider == provider);

        if (externalLogin is null)
            return Result.Failure(UserErrors.ExternalLoginNotFound);

        _externalLogins.Remove(externalLogin);
        SetModified();

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        RevokeAllRefreshTokens();
        SetModified();

        RaiseDomainEvent(new UserDeactivatedEvent(Id));
    }

    /// <summary>
    /// Activates the user account.
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        SetModified();
    }

    /// <inheritdoc />
    public override void SoftDelete(string? deletedBy = null)
    {
        if (IsDeleted)
            return;

        base.SoftDelete(deletedBy);
        IsActive = false;
        RevokeAllRefreshTokens();
    }
}
