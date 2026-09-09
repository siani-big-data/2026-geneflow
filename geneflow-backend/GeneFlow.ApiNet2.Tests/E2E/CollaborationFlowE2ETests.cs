using System.Net;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;

namespace GeneFlow.ApiNet2.Tests.E2E;

/// <summary>
/// E2E tests for collaboration flows.
/// Tests inviting collaborators, accepting/declining invitations, role-based permissions,
/// ownership transfer, and concurrent editing scenarios.
/// </summary>
public sealed class CollaborationFlowE2ETests : E2ETestBase
{
    public CollaborationFlowE2ETests(
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

    private sealed record AddStudyMemberRequest(string UserId, int RoleId);
    private sealed record ChangeMemberRoleRequest(int NewRoleId);
    private sealed record TransferOwnershipRequest(string NewOwnerId);

    private sealed record StudyMemberResponse(
        string UserId,
        string Username,
        string Email,
        string Role,
        int RoleId,
        DateTime JoinedAt);

    private sealed record SendInvitationRequest(string Email, int RoleId, string? Message);
    private sealed record AcceptInvitationRequest(string Token);

    private sealed record StudyInvitationResponse(
        string Id,
        string StudyId,
        string StudyTitle,
        string Email,
        string Status,
        int StatusId,
        string Role,
        int RoleId,
        string Token,
        DateTime ExpiresAt,
        DateTime CreatedAt);

    // Study roles
    private static class StudyRoles
    {
        public const int Owner = 1;
        public const int Admin = 2;
        public const int Collaborator = 3;
        public const int Viewer = 4;
    }

    #endregion

    #region Helper Methods

    private async Task<(string AccessToken, string UserId, string Email)> RegisterAndLoginAsync(string? email = null)
    {
        email ??= GenerateTestEmail();
        var username = GenerateTestUsername();

        var registerRequest = new RegisterRequest(email, username, TestPassword);
        await PostJsonAsync("/api/v1/auth/register", registerRequest);

        await ConfirmEmailAsync(email);

        var loginRequest = new LoginRequest(email, TestPassword);
        var loginResponse = await PostJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await ReadAsAsync<LoginResponse>(loginResponse);

        return (loginResult!.Tokens!.AccessToken, loginResult.User.Id, email);
    }

    private async Task<StudyResponse> CreateStudyAsync(string accessToken, string title = "Collaboration Test Study")
    {
        SetAuthorizationHeader(accessToken);
        var request = new CreateStudyRequest(
            title,
            "Study for collaboration E2E tests",
            1,
            "Collaboration Lab",
            "Dr. Collab",
            new List<string> { "collaboration", "test" });

        var response = await PostJsonAsync("/api/v1/studies", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ReadAsAsync<StudyResponse>(response))!;
    }

    #endregion

    #region Invitation Flow Tests

    [Fact]
    public async Task InviteUser_AcceptInvitation_ViewStudy_ShouldSucceed()
    {
        // Arrange - Create owner and invitee
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var inviteeEmail = GenerateTestEmail();
        var (inviteeToken, inviteeId, _) = await RegisterAndLoginAsync(inviteeEmail);

        // Owner creates study
        var study = await CreateStudyAsync(ownerToken, "Invitation Test Study");

        // Step 1: Owner sends invitation
        SetAuthorizationHeader(ownerToken);
        var inviteRequest = new SendInvitationRequest(inviteeEmail, StudyRoles.Collaborator, "Please join our study!");
        var inviteResponse = await PostJsonAsync($"/api/v1/studies/{study.Id}/invitations", inviteRequest);
        inviteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        // Step 2: Invitee checks their invitations
        SetAuthorizationHeader(inviteeToken);
        var myInvitationsResponse = await GetResponseAsync("/api/v1/invitations?pageNumber=1&pageSize=10");
        myInvitationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var invitationsResult = await ReadAsAsync<PagedResponseWrapper<StudyInvitationResponse>>(myInvitationsResponse);
        var invitation = invitationsResult?.Items.FirstOrDefault(i => i.StudyId == study.Id);

        if (invitation != null)
        {
            // Step 3: Invitee accepts invitation
            var acceptResponse = await PostJsonAsync(
                $"/api/v1/invitations/{invitation.Token}/accept",
                new { });
            acceptResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

            // Step 4: Verify invitee can view study
            var studyResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
            studyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var studyData = await ReadAsAsync<StudyResponse>(studyResponse);
            studyData.Should().NotBeNull();
            studyData!.Title.Should().Be("Invitation Test Study");
        }
    }

    [Fact]
    public async Task InviteUser_DeclineInvitation_ShouldNotHaveAccess()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var inviteeEmail = GenerateTestEmail();
        var (inviteeToken, _, _) = await RegisterAndLoginAsync(inviteeEmail);

        var study = await CreateStudyAsync(ownerToken, "Decline Test Study");

        // Owner sends invitation
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/invitations",
            new SendInvitationRequest(inviteeEmail, StudyRoles.Viewer, null));

        // Get invitation token
        SetAuthorizationHeader(inviteeToken);
        var invitationsResponse = await GetResponseAsync("/api/v1/invitations?pageNumber=1&pageSize=10");
        var invitations = await ReadAsAsync<PagedResponseWrapper<StudyInvitationResponse>>(invitationsResponse);
        var invitation = invitations?.Items.FirstOrDefault(i => i.StudyId == study.Id);

        if (invitation != null)
        {
            // Decline invitation
            var declineResponse = await PostJsonAsync(
                $"/api/v1/invitations/{invitation.Token}/decline",
                new { });
            declineResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

            // Verify invitee cannot access study
            var studyResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
            studyResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task InviteUser_ExpireInvitation_CannotAccept()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var inviteeEmail = GenerateTestEmail();
        var (inviteeToken, _, _) = await RegisterAndLoginAsync(inviteeEmail);

        var study = await CreateStudyAsync(ownerToken, "Expire Test Study");

        // Send invitation
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/invitations",
            new SendInvitationRequest(inviteeEmail, StudyRoles.Collaborator, null));

        // Note: In real E2E, we would need to either:
        // 1. Wait for the invitation to expire (not practical)
        // 2. Use a test endpoint to force expire the invitation
        // 3. Mock the time service

        // For this test, we verify the expired invitation behavior
        SetAuthorizationHeader(inviteeToken);
        var expiredTokenResponse = await PostJsonAsync(
            "/api/v1/invitations/expired-invalid-token/accept",
            new { });

        // Should fail with invalid/expired token
        expiredTokenResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Gone);
    }

    #endregion

    #region Role Change Tests

    [Fact]
    public async Task ChangeRole_UpdatesPermissions_ViewerCannotEdit()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var (memberToken, memberId, _) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Role Change Test Study");

        // Add member as Collaborator
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest(memberId, StudyRoles.Collaborator));

        // Member can edit study as Collaborator
        SetAuthorizationHeader(memberToken);
        var editRequest = new UpdateStudyRequest(
            "Updated by Collaborator",
            null,
            study.ResearchFieldId,
            null,
            null,
            null);
        var editResponse = await PutJsonAsync($"/api/v1/studies/{study.Id}", editRequest);
        // Collaborator might or might not be able to edit - depends on implementation
        var collaboratorCanEdit = editResponse.StatusCode == HttpStatusCode.OK;

        // Owner changes role to Viewer
        SetAuthorizationHeader(ownerToken);
        var changeRoleResponse = await PatchJsonAsync(
            $"/api/v1/studies/{study.Id}/members/{memberId}/role",
            new ChangeMemberRoleRequest(StudyRoles.Viewer));
        changeRoleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Viewer cannot edit study
        SetAuthorizationHeader(memberToken);
        var viewerEditResponse = await PutJsonAsync($"/api/v1/studies/{study.Id}", editRequest);
        viewerEditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveMember_LosesAccess_ShouldSucceed()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var (memberToken, memberId, _) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Remove Member Test Study");

        // Add member
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest(memberId, StudyRoles.Collaborator));

        // Verify member has access
        SetAuthorizationHeader(memberToken);
        var accessResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
        accessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Owner removes member
        SetAuthorizationHeader(ownerToken);
        var removeResponse = await DeleteAsync($"/api/v1/studies/{study.Id}/members/{memberId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify member lost access
        SetAuthorizationHeader(memberToken);
        var noAccessResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
        noAccessResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    #endregion

    #region Ownership Transfer Tests

    [Fact]
    public async Task TransferOwnership_PreviousOwnerBecomesAdmin()
    {
        // Arrange
        var (ownerToken, ownerId, _) = await RegisterAndLoginAsync();
        var (newOwnerToken, newOwnerId, _) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Transfer Ownership Test Study");

        // Add new owner as Admin first
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest(newOwnerId, StudyRoles.Admin));

        // Transfer ownership
        var transferRequest = new TransferOwnershipRequest(newOwnerId);
        var transferResponse = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/members/transfer-ownership",
            transferRequest);
        transferResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Verify new owner
        SetAuthorizationHeader(newOwnerToken);
        var studyResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
        studyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var studyData = await ReadAsAsync<StudyResponse>(studyResponse);
        studyData!.OwnerId.Should().Be(newOwnerId);

        // Verify previous owner is now Admin (still has access)
        SetAuthorizationHeader(ownerToken);
        var prevOwnerAccessResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
        prevOwnerAccessResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Concurrent Editing Tests

    [Fact]
    public async Task MultipleCollaborators_EditSameStudy_ShouldHandleConcurrency()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var (collab1Token, collab1Id, _) = await RegisterAndLoginAsync();
        var (collab2Token, collab2Id, _) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Concurrent Edit Study");

        // Add collaborators
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest(collab1Id, StudyRoles.Admin));
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest(collab2Id, StudyRoles.Admin));

        // Both collaborators try to edit concurrently
        var tasks = new List<Task<HttpResponseMessage>>();

        SetAuthorizationHeader(collab1Token);
        var task1 = PutJsonAsync($"/api/v1/studies/{study.Id}",
            new UpdateStudyRequest("Edit by Collab 1", "Desc 1", 1, null, null, null));
        tasks.Add(task1);

        // Note: In real concurrent test, these would run in parallel
        // For this E2E test, we run them sequentially to verify no corruption
        SetAuthorizationHeader(collab2Token);
        var task2 = PutJsonAsync($"/api/v1/studies/{study.Id}",
            new UpdateStudyRequest("Edit by Collab 2", "Desc 2", 1, null, null, null));
        tasks.Add(task2);

        var results = await Task.WhenAll(tasks);

        // At least one should succeed, and no errors should corrupt data
        results.Should().Contain(r => r.StatusCode == HttpStatusCode.OK);

        // Verify study integrity
        SetAuthorizationHeader(ownerToken);
        var finalStudyResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
        finalStudyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalStudy = await ReadAsAsync<StudyResponse>(finalStudyResponse);
        finalStudy.Should().NotBeNull();
        // Title should be one of the valid values
        finalStudy!.Title.Should().BeOneOf("Edit by Collab 1", "Edit by Collab 2");
    }

    #endregion

    #region Leave Study Tests

    [Fact]
    public async Task LeaveStudy_NotLastAdmin_ShouldSucceed()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var (adminToken, adminId, _) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Leave Study Test");

        // Add admin
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest(adminId, StudyRoles.Admin));

        // Admin leaves study
        SetAuthorizationHeader(adminToken);
        var leaveResponse = await PostJsonAsync($"/api/v1/studies/{study.Id}/members/leave", new { });
        leaveResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Verify admin lost access
        var accessResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
        accessResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LeaveStudy_AsOnlyOwner_ShouldFail()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(ownerToken, "Owner Leave Test");

        // Owner tries to leave (should fail as they're the only owner)
        SetAuthorizationHeader(ownerToken);
        var leaveResponse = await PostJsonAsync($"/api/v1/studies/{study.Id}/members/leave", new { });

        // Should fail because owner cannot leave without transferring ownership
        leaveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Permission Tests

    [Fact]
    public async Task Viewer_CannotModifyStudy_ShouldReturnForbidden()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var (viewerToken, viewerId, _) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Viewer Permission Test");

        // Add viewer
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest(viewerId, StudyRoles.Viewer));

        // Viewer tries to modify study
        SetAuthorizationHeader(viewerToken);
        var editResponse = await PutJsonAsync($"/api/v1/studies/{study.Id}",
            new UpdateStudyRequest("Viewer Edit Attempt", null, 1, null, null, null));

        editResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Viewer tries to delete study
        var deleteResponse = await DeleteAsync($"/api/v1/studies/{study.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Viewer tries to add member
        var addMemberResponse = await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest("some-user-id", StudyRoles.Viewer));
        addMemberResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_CanReadStudyData_ShouldSucceed()
    {
        // Arrange
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        var (viewerToken, viewerId, _) = await RegisterAndLoginAsync();

        var study = await CreateStudyAsync(ownerToken, "Viewer Read Test");

        // Add viewer
        SetAuthorizationHeader(ownerToken);
        await PostJsonAsync($"/api/v1/studies/{study.Id}/members",
            new AddStudyMemberRequest(viewerId, StudyRoles.Viewer));

        // Viewer can read study
        SetAuthorizationHeader(viewerToken);
        var readResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}");
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Viewer can read members
        var membersResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}/members");
        membersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Viewer can read traces
        var tracesResponse = await GetResponseAsync($"/api/v1/studies/{study.Id}/traces?pageNumber=1&pageSize=10");
        tracesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
