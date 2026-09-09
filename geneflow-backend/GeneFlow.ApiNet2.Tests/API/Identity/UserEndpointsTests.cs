using System.Net;
using System.Net.Http.Json;
using GeneFlow.ApiNet2.API.Contracts.Identity.Requests;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using NSubstitute.ReturnsExtensions;

namespace GeneFlow.ApiNet2.Tests.API.Identity;

/// <summary>
/// Integration tests for user management endpoints.
/// </summary>
public class UserEndpointsTests : IClassFixture<GeneFlowWebApplicationFactory>, IDisposable
{
    private readonly GeneFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserEndpointsTests(GeneFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ResetMocks();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    #region Get Current User Tests

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get User By Id Tests

    [Fact]
    public async Task GetUserById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var userId = "U00000001";

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Email Verification Tests

    [Fact]
    public async Task VerifyEmail_WithValidToken_ShouldReturnNoContent()
    {
        // Arrange
        var user = CreateTestUser(new UserId(1), "test@example.com", "testuser");
        var actualToken = user.EmailVerificationToken!;

        var request = new VerifyEmailRequest
        {
            Token = actualToken
        };

        _factory.MockUserRepository
            .GetByEmailVerificationTokenAsync(request.Token, Arg.Any<CancellationToken>())
            .Returns(user);

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users/verify-email", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        var request = new VerifyEmailRequest
        {
            Token = "invalid_token"
        };

        _factory.MockUserRepository
            .GetByEmailVerificationTokenAsync(request.Token, Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users/verify-email", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VerifyEmailByLink_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        var user = CreateTestUser(new UserId(1), "test@example.com", "testuser");
        var token = user.EmailVerificationToken!;

        _factory.MockUserRepository
            .GetByEmailVerificationTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(user);

        _factory.MockUserUnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        // Act
        var response = await _client.GetAsync($"/api/v1/users/verify-email?token={token}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyEmailByLink_WithMissingToken_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users/verify-email");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmailByLink_WithEmptyToken_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users/verify-email?token=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Two-Factor Authentication Tests

    [Fact]
    public async Task EnableTwoFactor_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/users/2fa/enable", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DisableTwoFactor_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/users/2fa/disable", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Health Check Test

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

    #endregion
}
