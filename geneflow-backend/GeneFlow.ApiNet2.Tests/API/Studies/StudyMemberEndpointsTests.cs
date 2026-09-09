using System.Net;
using System.Net.Http.Json;
using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

namespace GeneFlow.ApiNet2.Tests.API.Studies;

/// <summary>
/// Integration tests for study member endpoints.
/// </summary>
public class StudyMemberEndpointsTests : IClassFixture<GeneFlowWebApplicationFactory>, IDisposable
{
    private readonly GeneFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    private const string TestStudyId = "S00000001";
    private const string TestUserId = "U00000002";

    public StudyMemberEndpointsTests(GeneFlowWebApplicationFactory factory)
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

    #region GET /api/v1/studies/{studyId}/members

    [Fact]
    public async Task GetStudyMembers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/studies/{TestStudyId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStudyMembers_WithValidStudyId_ShouldReturnOk()
    {
        // Arrange
        _factory.SetupGetMembersSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/v1/studies/{TestStudyId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStudyMembers_WithValidStudyId_ShouldReturnMemberList()
    {
        // Arrange
        _factory.SetupGetMembersSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/v1/studies/{TestStudyId}/members");
        var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<StudyMemberResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStudyMembers_WithNonExistentStudy_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupStudyNotFound("S99999999");

        // Act
        var response = await _authenticatedClient.GetAsync("/api/v1/studies/S99999999/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/v1/studies/{studyId}/members

    [Fact]
    public async Task AddStudyMember_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new AddStudyMemberRequest
        {
            UserId = TestUserId,
            RoleId = 3 // Editor
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddStudyMember_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new AddStudyMemberRequest
        {
            UserId = TestUserId,
            RoleId = 3 // Editor
        };

        _factory.SetupAddMemberSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddStudyMember_WithInvalidRoleId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AddStudyMemberRequest
        {
            UserId = TestUserId,
            RoleId = 99 // Invalid role
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddStudyMember_WhenAlreadyMember_ShouldReturnConflict()
    {
        // Arrange
        var request = new AddStudyMemberRequest
        {
            UserId = TestUserId,
            RoleId = 3
        };

        _factory.SetupMemberAlreadyExists(TestStudyId, TestUserId);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region DELETE /api/v1/studies/{studyId}/members/{userId}

    [Fact]
    public async Task RemoveStudyMember_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/studies/{TestStudyId}/members/{TestUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveStudyMember_WithValidData_ShouldReturnNoContent()
    {
        // Arrange
        _factory.SetupRemoveMemberSuccess(TestStudyId, TestUserId);

        // Act
        var response = await _authenticatedClient.DeleteAsync($"/api/v1/studies/{TestStudyId}/members/{TestUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveStudyMember_WhenNotMember_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupMemberNotFound(TestStudyId, "U99999999");

        // Act
        var response = await _authenticatedClient.DeleteAsync($"/api/v1/studies/{TestStudyId}/members/U99999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PATCH /api/v1/studies/{studyId}/members/{userId}/role

    [Fact]
    public async Task ChangeMemberRole_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new ChangeMemberRoleRequest { NewRoleId = 2 };

        // Act
        var response = await _client.PatchAsJsonAsync(
            $"/api/v1/studies/{TestStudyId}/members/{TestUserId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeMemberRole_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new ChangeMemberRoleRequest { NewRoleId = 2 }; // Admin

        _factory.SetupChangeMemberRoleSuccess(TestStudyId, TestUserId);

        // Act
        var response = await _authenticatedClient.PatchAsJsonAsync(
            $"/api/v1/studies/{TestStudyId}/members/{TestUserId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/v1/studies/{studyId}/members/leave

    [Fact]
    public async Task LeaveStudy_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync($"/api/v1/studies/{TestStudyId}/members/leave", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LeaveStudy_WhenMember_ShouldReturnNoContent()
    {
        // Arrange
        _factory.SetupLeaveStudySuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.PostAsync($"/api/v1/studies/{TestStudyId}/members/leave", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region POST /api/v1/studies/{studyId}/members/transfer-ownership

    [Fact]
    public async Task TransferOwnership_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new TransferOwnershipRequest { NewOwnerId = TestUserId };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/studies/{TestStudyId}/members/transfer-ownership", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TransferOwnership_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new TransferOwnershipRequest { NewOwnerId = TestUserId };

        _factory.SetupTransferOwnershipSuccess(TestStudyId, TestUserId);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/v1/studies/{TestStudyId}/members/transfer-ownership", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
