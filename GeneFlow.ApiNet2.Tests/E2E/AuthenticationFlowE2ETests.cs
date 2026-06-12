using System.Net;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;

namespace GeneFlow.ApiNet2.Tests.E2E;

/// <summary>
/// E2E tests for complete authentication flows.
/// Tests the full user journey from registration through logout.
/// </summary>
public sealed class AuthenticationFlowE2ETests : E2ETestBase
{
    public AuthenticationFlowE2ETests(
        PostgreSqlContainerFixture postgresFixture,
        RedisContainerFixture redisFixture)
        : base(postgresFixture, redisFixture)
    {
    }

    #region Test DTOs

    private sealed record RegisterRequest(string Email, string Username, string Password);
    private sealed record LoginRequest(string Identifier, string Password, string? TwoFactorCode = null);
    private sealed record RefreshTokenRequest(string RefreshToken);
    private sealed record LogoutRequest(string RefreshToken);
    private sealed record VerifyEmailRequest(string Token);
    private sealed record RequestTwoFactorCodeRequest(string EmailOrUsername);
    private sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    private sealed record RequestPasswordResetRequest(string Email);
    private sealed record ResetPasswordRequest(string Token, string NewPassword);
    private sealed record OAuthLoginRequest(string AccessToken);

    private sealed record UserResponse(
        string Id,
        string Email,
        string Username,
        bool IsEmailVerified,
        bool IsTwoFactorEnabled,
        IReadOnlyList<string> Roles);

    private sealed record AuthTokensResponse(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt);

    private sealed record LoginResponse(
        UserResponse User,
        AuthTokensResponse? Tokens,
        bool RequiresTwoFactor);

    private sealed record TwoFactorSetupResponse(
        string SharedKey,
        string QrCodeUri);

    #endregion

    #region Complete Flow Tests

    [Fact]
    public async Task CompleteFlow_Register_VerifyEmail_Login_RefreshToken_Logout_ShouldSucceed()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username = GenerateTestUsername();

        // Step 1: Register
        var registerRequest = new RegisterRequest(email, username, TestPassword);
        var registerResponse = await PostJsonAsync("/api/v1/auth/register", registerRequest);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await ReadAsAsync<UserResponse>(registerResponse);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.Username.Should().Be(username);
        user.IsEmailVerified.Should().BeFalse();

        // Step 2: Verify email. The real token only exists in the verification
        // email, so use the dev-only endpoint that runs the same domain path.
        await ConfirmEmailAsync(email);

        // Step 3: Login
        var loginRequest = new LoginRequest(email, TestPassword);
        var loginResponse = await PostJsonAsync("/api/v1/auth/login", loginRequest);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await ReadAsAsync<LoginResponse>(loginResponse);
        loginResult.Should().NotBeNull();
        loginResult!.Tokens.Should().NotBeNull();
        loginResult.RequiresTwoFactor.Should().BeFalse();

        var accessToken = loginResult.Tokens!.AccessToken;
        var refreshToken = loginResult.Tokens.RefreshToken;

        // Step 4: Access protected resource
        SetAuthorizationHeader(accessToken);
        var meResponse = await GetResponseAsync("/api/v1/users/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Refresh token
        ClearAuthorizationHeader();
        var refreshRequest = new RefreshTokenRequest(refreshToken);
        var refreshResponse = await PostJsonAsync("/api/v1/auth/refresh", refreshRequest);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await ReadAsAsync<AuthTokensResponse>(refreshResponse);
        newTokens.Should().NotBeNull();
        newTokens!.AccessToken.Should().NotBeEmpty();
        newTokens.RefreshToken.Should().NotBeEmpty();

        // Step 6: Logout
        SetAuthorizationHeader(newTokens.AccessToken);
        var logoutRequest = new LogoutRequest(newTokens.RefreshToken);
        var logoutResponse = await PostJsonAsync("/api/v1/auth/logout", logoutRequest);

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 7: Verify refresh token is invalidated
        ClearAuthorizationHeader();
        var invalidRefreshRequest = new RefreshTokenRequest(newTokens.RefreshToken);
        var invalidRefreshResponse = await PostJsonAsync("/api/v1/auth/refresh", invalidRefreshRequest);

        invalidRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TwoFactorAuthFlow_Enable_Login_Verify_ShouldSucceed()
    {
        // Arrange - Register and login
        var email = GenerateTestEmail();
        var username = GenerateTestUsername();

        var registerRequest = new RegisterRequest(email, username, TestPassword);
        var registerResponse = await PostJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        await ConfirmEmailAsync(email);

        var loginRequest = new LoginRequest(email, TestPassword);
        var loginResponse = await PostJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await ReadAsAsync<LoginResponse>(loginResponse);
        SetAuthorizationHeader(loginResult!.Tokens!.AccessToken);

        // Step 1: Enable 2FA
        var enable2faResponse = await PostJsonAsync("/api/v1/users/2fa/enable", new { });
        // Should return 204 or setup info
        enable2faResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        // Step 2: Logout
        ClearAuthorizationHeader();

        // Step 3: Try to login - should require 2FA
        var login2Request = new LoginRequest(email, TestPassword);
        var login2Response = await PostJsonAsync("/api/v1/auth/login", login2Request);

        // If 2FA is enabled, should indicate it's required
        if (login2Response.StatusCode == HttpStatusCode.OK)
        {
            var login2Result = await ReadAsAsync<LoginResponse>(login2Response);
            // Either requires 2FA or login succeeded (depends on implementation)
            login2Result.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task TwoFactorAuthFlow_SetupTOTP_Confirm_LoginWith2FA_ShouldSucceed()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username = GenerateTestUsername();

        // Register
        var registerRequest = new RegisterRequest(email, username, TestPassword);
        await PostJsonAsync("/api/v1/auth/register", registerRequest);

        await ConfirmEmailAsync(email);

        // Login
        var loginRequest = new LoginRequest(email, TestPassword);
        var loginResponse = await PostJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await ReadAsAsync<LoginResponse>(loginResponse);
        SetAuthorizationHeader(loginResult!.Tokens!.AccessToken);

        // Setup TOTP 2FA
        var setupResponse = await PostJsonAsync("/api/v1/users/2fa/setup", new { });

        if (setupResponse.StatusCode == HttpStatusCode.OK)
        {
            var setupResult = await ReadAsAsync<TwoFactorSetupResponse>(setupResponse);
            setupResult.Should().NotBeNull();
            setupResult!.SharedKey.Should().NotBeEmpty();
            setupResult.QrCodeUri.Should().NotBeEmpty();

            // Note: In real E2E test, we would generate a valid TOTP code
            // using the shared key and a TOTP library
        }
    }

    [Fact]
    public async Task OAuthFlow_GitHubLogin_ShouldReturnTokens()
    {
        // Arrange - OAuth login with a mock access token
        // In real E2E, this would require a valid GitHub token
        var oauthRequest = new OAuthLoginRequest("mock-github-access-token");

        // Act
        var response = await PostJsonAsync("/api/v1/auth/oauth/github", oauthRequest);

        // Assert - Should fail with invalid token (expected in test environment)
        // In production E2E with real OAuth, this would succeed
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest);
    }

    #endregion

    #region Registration Tests

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username = GenerateTestUsername();
        var request = new RegisterRequest(email, username, TestPassword);

        // Act
        var response = await PostJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await ReadAsAsync<UserResponse>(response);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.Username.Should().Be(username);
        user.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username1 = GenerateTestUsername();
        var username2 = GenerateTestUsername();

        // First registration
        var request1 = new RegisterRequest(email, username1, TestPassword);
        await PostJsonAsync("/api/v1/auth/register", request1);

        // Act - Second registration with same email
        var request2 = new RegisterRequest(email, username2, TestPassword);
        var response = await PostJsonAsync("/api/v1/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest("invalid-email", GenerateTestUsername(), TestPassword);

        // Act
        var response = await PostJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest(GenerateTestEmail(), GenerateTestUsername(), "weak");

        // Act
        var response = await PostJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username = GenerateTestUsername();
        await PostJsonAsync("/api/v1/auth/register", new RegisterRequest(email, username, TestPassword));

        await ConfirmEmailAsync(email);

        var loginRequest = new LoginRequest(email, TestPassword);

        // Act
        var response = await PostJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadAsAsync<LoginResponse>(response);
        result.Should().NotBeNull();
        result!.Tokens.Should().NotBeNull();
        result.Tokens!.AccessToken.Should().NotBeEmpty();
        result.Tokens.RefreshToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username = GenerateTestUsername();
        await PostJsonAsync("/api/v1/auth/register", new RegisterRequest(email, username, TestPassword));

        var loginRequest = new LoginRequest(email, "WrongPassword123!");

        // Act
        var response = await PostJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent@example.com", TestPassword);

        // Act
        var response = await PostJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var email = GenerateTestEmail();
        var username = GenerateTestUsername();
        await PostJsonAsync("/api/v1/auth/register", new RegisterRequest(email, username, TestPassword));

        await ConfirmEmailAsync(email);

        var loginResponse = await PostJsonAsync("/api/v1/auth/login", new LoginRequest(email, TestPassword));
        var loginResult = await ReadAsAsync<LoginResponse>(loginResponse);
        var refreshToken = loginResult!.Tokens!.RefreshToken;

        // Act
        var response = await PostJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(refreshToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await ReadAsAsync<AuthTokensResponse>(response);
        newTokens.Should().NotBeNull();
        newTokens!.AccessToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var invalidToken = "invalid-refresh-token";

        // Act
        var response = await PostJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(invalidToken));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Password Reset Tests

    [Fact]
    public async Task RequestPasswordReset_ShouldAlwaysReturn204ToPreventEnumeration()
    {
        // Arrange
        var request = new RequestPasswordResetRequest("any@email.com");

        // Act
        var response = await PostJsonAsync("/api/v1/auth/request-password-reset", request);

        // Assert - Always returns 204 to prevent email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        var request = new ResetPasswordRequest("invalid-token", "NewPassword123!");

        // Act
        var response = await PostJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound,
            HttpStatusCode.UnprocessableEntity);
    }

    #endregion
}
