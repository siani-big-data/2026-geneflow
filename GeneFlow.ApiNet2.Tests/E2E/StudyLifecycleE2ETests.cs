using System.Net;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;

namespace GeneFlow.ApiNet2.Tests.E2E;

/// <summary>
/// E2E tests for complete study lifecycle flows.
/// Tests creating studies, managing members, invitations, and archiving.
/// </summary>
public sealed class StudyLifecycleE2ETests : E2ETestBase
{
    public StudyLifecycleE2ETests(
        PostgreSqlContainerFixture postgresFixture,
        RedisContainerFixture redisFixture)
        : base(postgresFixture, redisFixture)
    {
    }

    #region Test DTOs

    private sealed record RegisterRequest(string Email, string Username, string Password);
    private sealed record LoginRequest(string Identifier, string Password, string? TwoFactorCode = null);

    private sealed record LoginResponse(
        UserResponse User,
        AuthTokensResponse? Tokens,
        bool RequiresTwoFactor);

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

    private sealed record CreateStudyRequest(
        string Title,
        string? Description,
        int ResearchFieldId,
        string? Institution,
        string? PrincipalInvestigator,
        IReadOnlyList<string>? Tags);

    private sealed record UpdateStudyRequest(
        string Title,
        string? Description,
        int ResearchFieldId,
        string? Institution,
        string? PrincipalInvestigator,
        IReadOnlyList<string>? Tags);

    private sealed record ChangeStudyStatusRequest(int NewStatusId);

    private sealed record StudyResponse(
        string Id,
        string Title,
        string? Description,
        string Status,
        int StatusId,
        string ResearchField,
        int ResearchFieldId,
        string? Institution,
        string? PrincipalInvestigator,
        IReadOnlyList<string> Tags,
        string OwnerId,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    private sealed record StudySummaryResponse(
        string Id,
        string Title,
        string Status,
        string ResearchField,
        int TraceCount,
        int MemberCount,
        DateTime CreatedAt);

    private sealed record AddStudyMemberRequest(string UserId, int RoleId);
    private sealed record ChangeMemberRoleRequest(int NewRoleId);

    private sealed record StudyMemberResponse(
        string UserId,
        string Username,
        string Email,
        string Role,
        int RoleId,
        DateTime JoinedAt);

    private sealed record SendInvitationRequest(string Email, int RoleId, string? Message);

    private sealed record StudyInvitationResponse(
        string Id,
        string Email,
        string Status,
        string Role,
        DateTime ExpiresAt,
        DateTime CreatedAt);

    private sealed record ResearchFieldResponse(int Id, string Name, string? Description);

    #endregion

    #region Helper Methods

    private async Task<(string AccessToken, string UserId)> RegisterAndLoginAsync(string? email = null, string? username = null)
    {
        email ??= GenerateTestEmail();
        username ??= GenerateTestUsername();

        var registerRequest = new RegisterRequest(email, username, TestPassword);
        await PostJsonAsync("/api/v1/auth/register", registerRequest);

        await ConfirmEmailAsync(email);

        var loginRequest = new LoginRequest(email, TestPassword);
        var loginResponse = await PostJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await ReadAsAsync<LoginResponse>(loginResponse);

        return (loginResult!.Tokens!.AccessToken, loginResult.User.Id);
    }

    private async Task<StudyResponse> CreateStudyAsync(string accessToken, string title)
    {
        SetAuthorizationHeader(accessToken);
        var request = new CreateStudyRequest(
            title,
            "Test study description",
            1, // Molecular Biology
            "Test University",
            "Dr. Test",
            new List<string> { "test", "e2e" });

        var response = await PostJsonAsync("/api/v1/studies", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ReadAsAsync<StudyResponse>(response))!;
    }

    #endregion

    #region Complete Lifecycle Tests

    [Fact]
    public async Task CompleteLifecycle_CreateStudy_AddMembers_Invite_Archive_ShouldSucceed()
    {
        // Arrange - Create owner and collaborator accounts
        var (ownerToken, ownerId) = await RegisterAndLoginAsync();
        var (collaboratorToken, collaboratorId) = await RegisterAndLoginAsync();

        // Step 1: Create study
        var study = await CreateStudyAsync(ownerToken, "Lifecycle Test Study");
        study.Should().NotBeNull();
        study.Title.Should().Be("Lifecycle Test Study");
        study.OwnerId.Should().Be(ownerId);

        // Step 2: Add member directly
        SetAuthorizationHeader(ownerToken);
        var addMemberRequest = new AddStudyMemberRequest(collaboratorId, 2); // Collaborator role
        var addMemberResponse = await PostJsonAsync($"/api/v1/studies/{study.Id}/members", addMemberRequest);
        addMemberResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        // Step 3: Get study members
        var membersResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}/members");
        membersResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await ReadAsAsync<IReadOnlyList<StudyMemberResponse>>(membersResponse);
        members.Should().NotBeNull();
        members!.Should().HaveCountGreaterOrEqualTo(1);

        // Step 4: Send invitation to another user
        var invitationRequest = new SendInvitationRequest("newuser@example.com", 3, "Please join our study"); // Viewer role
        var invitationResponse = await PostJsonAsync($"/api/v1/studies/{study.Id}/invitations", invitationRequest);
        invitationResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        // Step 5: Change study status to Archived. Statuses only transition along
        // Draft(1) -> Active(2) -> Completed(3) -> Published(4) -> Archived(5),
        // so walk the full lifecycle chain.
        foreach (var statusId in new[] { 2, 3, 4, 5 })
        {
            var statusRequest = new ChangeStudyStatusRequest(statusId);
            var statusResponse = await PatchJsonAsync($"/api/v1/studies/{study.Id}/status", statusRequest);
            statusResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"transition to status {statusId} should succeed");
        }

        // Step 6: Verify study is archived
        var archivedStudy = await GetAsync<StudyResponse>($"/api/v1/studies/{study.Id}");
        archivedStudy.Should().NotBeNull();
        archivedStudy!.StatusId.Should().Be(5);
    }

    [Fact]
    public async Task CreateStudy_UpdateStudy_DeleteStudy_ShouldSucceed()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();

        // Step 1: Create study
        var study = await CreateStudyAsync(accessToken, "CRUD Test Study");

        // Step 2: Update study
        SetAuthorizationHeader(accessToken);
        var updateRequest = new UpdateStudyRequest(
            "Updated Study Title",
            "Updated description",
            2, // Different research field
            "Updated University",
            "Dr. Updated",
            new List<string> { "updated", "tags" });

        var updateResponse = await PutJsonAsync($"/api/v1/studies/{study.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedStudy = await ReadAsAsync<StudyResponse>(updateResponse);
        updatedStudy!.Title.Should().Be("Updated Study Title");

        // Step 3: Delete study
        var deleteResponse = await DeleteAsync($"/api/v1/studies/{study.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Step 4: Verify study is deleted (should return 404)
        var getDeletedResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Study Creation Tests

    [Fact]
    public async Task CreateStudy_WithValidData_ShouldReturnCreatedStudy()
    {
        // Arrange
        var (accessToken, userId) = await RegisterAndLoginAsync();
        SetAuthorizationHeader(accessToken);

        var request = new CreateStudyRequest(
            "New Research Study",
            "A comprehensive study on molecular biology",
            1,
            "MIT",
            "Dr. Jane Smith",
            new List<string> { "genomics", "research" });

        // Act
        var response = await PostJsonAsync("/api/v1/studies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var study = await ReadAsAsync<StudyResponse>(response);
        study.Should().NotBeNull();
        study!.Title.Should().Be("New Research Study");
        study.OwnerId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateStudy_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthorizationHeader();
        var request = new CreateStudyRequest("Test Study", null, 1, null, null, null);

        // Act
        var response = await PostJsonAsync("/api/v1/studies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateStudy_WithEmptyTitle_ShouldReturnBadRequest()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        SetAuthorizationHeader(accessToken);

        var request = new CreateStudyRequest("", null, 1, null, null, null);

        // Act
        var response = await PostJsonAsync("/api/v1/studies", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Study Member Tests

    [Fact]
    public async Task AddMember_AsOwner_ShouldSucceed()
    {
        // Arrange
        var (ownerToken, _) = await RegisterAndLoginAsync();
        var (_, collaboratorId) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Member Test Study");

        // Act
        SetAuthorizationHeader(ownerToken);
        var request = new AddStudyMemberRequest(collaboratorId, 2); // Collaborator
        var response = await PostJsonAsync($"/api/v1/studies/{study.Id}/members", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task RemoveMember_AsOwner_ShouldSucceed()
    {
        // Arrange
        var (ownerToken, _) = await RegisterAndLoginAsync();
        var (_, collaboratorId) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Remove Member Test");
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members", new AddStudyMemberRequest(collaboratorId, 2));

        // Act
        var response = await DeleteAsync($"/api/v1/studies/{study.Id}/members/{collaboratorId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeMemberRole_AsOwner_ShouldSucceed()
    {
        // Arrange
        var (ownerToken, _) = await RegisterAndLoginAsync();
        var (_, collaboratorId) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Change Role Test");
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members", new AddStudyMemberRequest(collaboratorId, 3)); // Viewer

        // Act
        var request = new ChangeMemberRoleRequest(2); // Change to Collaborator
        var response = await PatchJsonAsync($"/api/v1/studies/{study.Id}/members/{collaboratorId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Study Invitation Tests

    [Fact]
    public async Task SendInvitation_AsOwner_ShouldSucceed()
    {
        // Arrange
        var (ownerToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(ownerToken, "Invitation Test Study");

        SetAuthorizationHeader(ownerToken);
        var request = new SendInvitationRequest("invitee@example.com", 2, "Join our research!");

        // Act
        var response = await PostJsonAsync($"/api/v1/studies/{study.Id}/invitations", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStudyInvitations_AsOwner_ShouldReturnInvitations()
    {
        // Arrange
        var (ownerToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(ownerToken, "List Invitations Test");

        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/invitations",
            new SendInvitationRequest("user1@example.com", 2, null));

        // Act
        var response = await GetResponseAsync($"/api/v1/studies/{study.Id}/invitations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Study Query Tests

    [Fact]
    public async Task GetUserStudies_ShouldReturnOwnedAndMemberStudies()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        await CreateStudyAsync(accessToken, "My Study 1");
        await CreateStudyAsync(accessToken, "My Study 2");

        // Act
        SetAuthorizationHeader(accessToken);
        var response = await GetResponseAsync("/api/v1/studies/mine?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadAsAsync<PagedResponseWrapper<StudySummaryResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetResearchFields_ShouldReturnFields()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        SetAuthorizationHeader(accessToken);

        // Act
        var response = await GetResponseAsync("/api/v1/studies/research-fields");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fields = await ReadAsAsync<IReadOnlyList<ResearchFieldResponse>>(response);
        fields.Should().NotBeNull();
        fields!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPublicStudies_ShouldReturnPublishedStudies()
    {
        // Act - No authentication required for public studies
        ClearAuthorizationHeader();
        var response = await GetResponseAsync("/api/v1/studies/public?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
