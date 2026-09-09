using System.Net;
using System.Net.Http.Json;
using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

namespace GeneFlow.ApiNet2.Tests.API.Studies;

/// <summary>
/// Integration tests for study invitation endpoints.
/// </summary>
public class StudyInvitationEndpointsTests : IClassFixture<GeneFlowWebApplicationFactory>, IDisposable
{
    private readonly GeneFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    private const string TestStudyId = "S00000001";
    private const string TestInvitationId = "I00000001";
    private const string TestToken = "valid-invitation-token-12345";
    private const string TestEmail = "invitee@example.com";

    public StudyInvitationEndpointsTests(GeneFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _authenticatedClient = factory.CreateAuthenticatedClient();
        _factory.ResetMocks();
    }

    public void Dispose()
    {
        _client.Dispose();
        _authenticatedClient.Dispose();
    }

    #region POST /api/v1/studies/{studyId}/invitations (Send Invitation)

    [Fact]
    public async Task SendInvitation_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new SendInvitationRequest
        {
            Email = TestEmail,
            RoleId = 3,
            Message = "Join our research study!"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/invitations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendInvitation_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new SendInvitationRequest
        {
            Email = TestEmail,
            RoleId = 3, // Editor
            Message = "Join our research study!"
        };

        _factory.SetupSendInvitationSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/v1/studies/{TestStudyId}/invitations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task SendInvitation_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SendInvitationRequest
        {
            Email = "invalid-email",
            RoleId = 3
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/v1/studies/{TestStudyId}/invitations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendInvitation_WithInvalidRoleId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SendInvitationRequest
        {
            Email = TestEmail,
            RoleId = 99 // Invalid role
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/v1/studies/{TestStudyId}/invitations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendInvitation_WhenAlreadyInvited_ShouldReturnConflict()
    {
        // Arrange
        var request = new SendInvitationRequest
        {
            Email = TestEmail,
            RoleId = 3
        };

        _factory.SetupInvitationAlreadyExists(TestStudyId, TestEmail);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/v1/studies/{TestStudyId}/invitations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region GET /api/v1/studies/{studyId}/invitations (Get Study Invitations)

    [Fact]
    public async Task GetStudyInvitations_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/studies/{TestStudyId}/invitations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStudyInvitations_WithValidStudyId_ShouldReturnOk()
    {
        // Arrange
        _factory.SetupGetInvitationsSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/studies/{TestStudyId}/invitations?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStudyInvitations_ShouldReturnPagedResponse()
    {
        // Arrange
        _factory.SetupGetInvitationsSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/studies/{TestStudyId}/invitations?pageNumber=1&pageSize=10");
        var content = await response.Content.ReadFromJsonAsync<PagedResponse<StudyInvitationResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
    }

    #endregion

    #region DELETE /api/v1/studies/{studyId}/invitations/{invitationId} (Cancel Invitation)

    [Fact]
    public async Task CancelInvitation_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync(
            $"/api/v1/studies/{TestStudyId}/invitations/{TestInvitationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelInvitation_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        _factory.SetupCancelInvitationSuccess(TestStudyId, TestInvitationId);

        // Act
        var response = await _authenticatedClient.DeleteAsync(
            $"/api/v1/studies/{TestStudyId}/invitations/{TestInvitationId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CancelInvitation_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupInvitationNotFound(TestStudyId, "I99999999");

        // Act
        var response = await _authenticatedClient.DeleteAsync(
            $"/api/v1/studies/{TestStudyId}/invitations/I99999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/v1/studies/{studyId}/invitations/{invitationId}/resend (Resend Invitation)

    [Fact]
    public async Task ResendInvitation_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync(
            $"/api/v1/studies/{TestStudyId}/invitations/{TestInvitationId}/resend", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResendInvitation_WithValidId_ShouldReturnOk()
    {
        // Arrange
        _factory.SetupResendInvitationSuccess(TestStudyId, TestInvitationId);

        // Act
        var response = await _authenticatedClient.PostAsync(
            $"/api/v1/studies/{TestStudyId}/invitations/{TestInvitationId}/resend", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/v1/invitations (Get My Invitations)

    [Fact]
    public async Task GetMyInvitations_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/invitations?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyInvitations_WhenAuthenticated_ShouldReturnOk()
    {
        // Arrange - the email is resolved from the authenticated user
        _factory.SetupGetMyInvitationsSuccess("testuser@example.com");

        // Act
        var response = await _authenticatedClient.GetAsync(
            "/api/v1/invitations?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyInvitations_IgnoresEmailQueryParameter()
    {
        // Arrange - even if a caller passes someone else's email, the endpoint
        // must use the authenticated user's email instead.
        _factory.SetupGetMyInvitationsSuccess("testuser@example.com");

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/v1/invitations?email={TestEmail}&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await _factory.MockStudyInvitationRepository.DidNotReceive().GetPendingByEmailAsync(
            TestEmail, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GET /api/v1/invitations/{token} (Get Invitation By Token)

    [Fact]
    public async Task GetInvitationByToken_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        _factory.SetupGetInvitationByTokenSuccess(TestToken);

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/v1/invitations/{TestToken}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetInvitationByToken_WithInvalidToken_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupInvitationTokenNotFound("invalid-token");

        // Act
        var response = await _authenticatedClient.GetAsync("/api/v1/invitations/invalid-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/v1/invitations/{token}/accept (Accept Invitation)

    [Fact]
    public async Task AcceptInvitation_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync($"/api/v1/invitations/{TestToken}/accept", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptInvitation_WithValidToken_ShouldReturnOk()
    {
        // Arrange
        _factory.SetupAcceptInvitationSuccess(TestToken);

        // Act
        var response = await _authenticatedClient.PostAsync($"/api/v1/invitations/{TestToken}/accept", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AcceptInvitation_WithInvalidToken_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupInvitationTokenNotFound("invalid-token");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/v1/invitations/invalid-token/accept", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/v1/invitations/{token}/decline (Decline Invitation)

    [Fact]
    public async Task DeclineInvitation_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync($"/api/v1/invitations/{TestToken}/decline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeclineInvitation_WithValidToken_ShouldReturnNoContent()
    {
        // Arrange
        _factory.SetupDeclineInvitationSuccess(TestToken);

        // Act
        var response = await _authenticatedClient.PostAsync($"/api/v1/invitations/{TestToken}/decline", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion
}
