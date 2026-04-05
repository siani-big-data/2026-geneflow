using System.Net;
using System.Net.Http.Json;
using GeneFlow.ApiNet2.API.Contracts.Identity.Requests;
using GeneFlow.ApiNet2.API.Contracts.Identity.Responses;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using NSubstitute.ReturnsExtensions;
using RefreshTokenVO = GeneFlow.ApiNet2.Domain.Identity.ValueObjects.RefreshToken;

namespace GeneFlow.ApiNet2.Tests.API.Identity;

/// <summary>
/// Integration tests for authentication endpoints.
/// </summary>
public class AuthEndpointsTests : IClassFixture<GeneFlowWebApplicationFactory>, IDisposable
{
    private readonly GeneFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(GeneFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ResetMocks();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "Password123!"
        };

        _factory.MockSequenceGenerator
            .NextAsync(UserId.SequenceName, Arg.Any<CancellationToken>())
            .Returns(1L);

        _factory.MockUserRepository
            .ExistsWithEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _factory.MockUserRepository
            .ExistsWithUsernameAsync(Arg.Any<Username>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _factory.MockPasswordHasher
            .Hash(request.Password)
            .Returns("hashed_password");

        _factory.MockUserRepository
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Username = "newuser",
            Password = "Password123!"
        };

        _factory.MockUserRepository
            .ExistsWithEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "username", "Password123!")]
    [InlineData("invalid-email", "username", "Password123!")]
    [InlineData("test@example.com", "", "Password123!")]
    [InlineData("test@example.com", "ab", "Password123!")]
    [InlineData("test@example.com", "username", "")]
    [InlineData("test@example.com", "username", "short")]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest(
        string email, string username, string password)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = email,
            Username = username,
            Password = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithTokens()
    {
        // Arrange
        var request = new LoginRequest
        {
            Identifier = "test@example.com",
            Password = "Password123!"
        };

        var userId = new UserId(1);
        var user = CreateTestUser(userId, request.Identifier, "testuser");

        _factory.MockUserRepository
            .GetByEmailOrUsernameAsync(request.Identifier, Arg.Any<CancellationToken>())
            .Returns(user);

        _factory.MockAuthValidator
            .ValidateCanAuthenticate(Arg.Any<User>())
            .Returns(Result.Success());

        _factory.MockAuthValidator
            .ValidatePassword(Arg.Any<User>(), Arg.Any<string>())
            .Returns(Result.Success());

        _factory.MockJwtTokenGenerator
            .GenerateAccessToken(Arg.Any<User>())
            .Returns(("access_token", DateTime.UtcNow.AddMinutes(15)));

        _factory.MockJwtTokenGenerator
            .GenerateRefreshToken()
            .Returns(("refresh_token", DateTime.UtcNow.AddDays(7)));

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.User.Should().NotBeNull();
        loginResponse.RequiresTwoFactor.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Identifier = "test@example.com",
            Password = "WrongPassword!"
        };

        var user = CreateTestUser(new UserId(1), request.Identifier, "testuser");

        _factory.MockUserRepository
            .GetByEmailOrUsernameAsync(request.Identifier, Arg.Any<CancellationToken>())
            .Returns(user);

        _factory.MockAuthValidator
            .ValidateCanAuthenticate(Arg.Any<User>())
            .Returns(Result.Success());

        _factory.MockAuthValidator
            .ValidatePassword(Arg.Any<User>(), Arg.Any<string>())
            .Returns(Result.Failure(UserErrors.InvalidCredentials));

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Identifier = "nonexistent@example.com",
            Password = "Password123!"
        };

        _factory.MockUserRepository
            .GetByEmailOrUsernameAsync(request.Identifier, Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid_refresh_token"
        };

        var user = CreateTestUser(new UserId(1), "test@example.com", "testuser");
        var refreshToken = RefreshTokenVO.Create(request.RefreshToken, DateTime.UtcNow.AddDays(7)).Value;
        user.AddRefreshToken(refreshToken);

        _factory.MockUserRepository
            .GetByRefreshTokenAsync(request.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(user);

        _factory.MockJwtTokenGenerator
            .GenerateAccessToken(Arg.Any<User>())
            .Returns(("new_access_token", DateTime.UtcNow.AddMinutes(15)));

        _factory.MockJwtTokenGenerator
            .GenerateRefreshToken()
            .Returns(("new_refresh_token", DateTime.UtcNow.AddDays(7)));

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<AuthTokensResponse>();
        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().Be("new_access_token");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnNotFound()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid_refresh_token"
        };

        _factory.MockUserRepository
            .GetByRefreshTokenAsync(request.RefreshToken, Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert - RefreshTokenNotFound is defined as NotFound error type
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LogoutRequest
        {
            RefreshToken = "some_token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Password Reset Tests

    [Fact]
    public async Task RequestPasswordReset_WithValidEmail_ShouldReturnNoContent()
    {
        // Arrange
        var request = new RequestPasswordResetRequest
        {
            Email = "test@example.com"
        };

        var user = CreateTestUser(new UserId(1), request.Email, "testuser");

        _factory.MockUserRepository
            .GetByEmailStringAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/request-password-reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RequestPasswordReset_WithNonExistentEmail_ShouldStillReturnNoContent()
    {
        // Arrange - Always return success to prevent email enumeration
        var request = new RequestPasswordResetRequest
        {
            Email = "nonexistent@example.com"
        };

        _factory.MockUserRepository
            .GetByEmailStringAsync(request.Email, Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/request-password-reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldReturnNoContent()
    {
        // Arrange
        var user = CreateTestUser(new UserId(1), "test@example.com", "testuser");
        var actualToken = user.RequestPasswordReset();

        var request = new ResetPasswordRequest
        {
            Token = actualToken,
            NewPassword = "NewPassword123!"
        };

        _factory.MockUserRepository
            .GetByPasswordResetTokenAsync(request.Token, Arg.Any<CancellationToken>())
            .Returns(user);

        _factory.MockPasswordHasher
            .Hash(request.NewPassword)
            .Returns("new_hashed_password");

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region 2FA Tests

    [Fact]
    public async Task RequestTwoFactorCode_ShouldReturnNoContent()
    {
        // Arrange
        var request = new RequestTwoFactorCodeRequest
        {
            EmailOrUsername = "test@example.com"
        };

        var user = CreateTestUserWith2FA(new UserId(1), request.EmailOrUsername, "testuser");

        _factory.MockUserRepository
            .GetByEmailOrUsernameAsync(request.EmailOrUsername, Arg.Any<CancellationToken>())
            .Returns(user);

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/2fa/request-code", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(UserId id, string email, string username)
    {
        var emailVo = Email.Create(email).Value;
        var usernameVo = Username.Create(username).Value;
        var passwordHash = PasswordHash.Create("hashed_password").Value;

        return User.Create(id, emailVo, usernameVo, passwordHash).Value;
    }

    private static User CreateTestUserWith2FA(UserId id, string email, string username)
    {
        var user = CreateTestUser(id, email, username);
        user.EnableTwoFactor();
        return user;
    }

    #endregion
}
