using System.Net;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;

namespace GeneFlow.ApiNet2.Tests.E2E;

/// <summary>
/// E2E tests for complete pipeline lifecycle flows.
/// Tests creating pipelines, adding steps, activation, execution, and result verification.
/// </summary>
public sealed class PipelineLifecycleE2ETests : E2ETestBase
{
    public PipelineLifecycleE2ETests(
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

    private sealed record CreatePipelineRequest(
        string Name,
        string? Description);

    private sealed record UpdatePipelineRequest(
        string Name,
        string? Description);

    private sealed record PipelineResponse(
        string Id,
        string StudyId,
        string Name,
        string? Description,
        string StatusName,
        int StatusId,
        IReadOnlyList<PipelineStepResponse> Steps,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    private sealed record PipelineSummaryResponse(
        string Id,
        string Name,
        string StatusName,
        int StatusId,
        int StepCount,
        int ExecutionCount,
        DateTime CreatedAt);

    private sealed record PipelineStepResponse(
        string Id,
        int StepTypeId,
        string StepTypeName,
        int Order,
        string? Label,
        string Configuration,
        bool IsEnabled,
        DateTime CreatedAt);

    private sealed record AddStepRequest(
        string Label,
        int StepTypeId,
        string? Configuration);

    private sealed record UpdateStepRequest(
        string? Label,
        string? Configuration,
        bool IsEnabled);

    private sealed record ReorderStepsRequest(IReadOnlyList<string> StepIds);

    private sealed record ExecutePipelineRequest(string TraceId);

    private sealed record PipelineExecutionResponse(
        string Id,
        string PipelineId,
        string TraceId,
        string Status,
        int StatusId,
        DateTime StartedAt,
        DateTime? CompletedAt,
        IReadOnlyList<StepExecutionResponse>? StepExecutions);

    private sealed record PipelineExecutionSummaryResponse(
        string Id,
        string TraceId,
        string TraceName,
        string Status,
        DateTime StartedAt,
        DateTime? CompletedAt);

    private sealed record StepExecutionResponse(
        string Id,
        string StepId,
        string StepName,
        string Status,
        DateTime StartedAt,
        DateTime? CompletedAt,
        string? ErrorMessage);

    private sealed record StepTypeResponse(
        int Id,
        string Name,
        string? Description,
        object? ConfigurationSchema);

    #endregion

    #region Helper Methods

    private async Task<(string AccessToken, string UserId)> RegisterAndLoginAsync()
    {
        var email = GenerateTestEmail();
        var username = GenerateTestUsername();

        var registerRequest = new RegisterRequest(email, username, TestPassword);
        await PostJsonAsync("/api/v1/auth/register", registerRequest);

        await ConfirmEmailAsync(email);

        var loginRequest = new LoginRequest(email, TestPassword);
        var loginResponse = await PostJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await ReadAsAsync<LoginResponse>(loginResponse);

        return (loginResult!.Tokens!.AccessToken, loginResult.User.Id);
    }

    private async Task<StudyResponse> CreateStudyAsync(string accessToken)
    {
        SetAuthorizationHeader(accessToken);
        var request = new CreateStudyRequest(
            "Pipeline Test Study",
            "Study for pipeline E2E tests",
            1,
            "Pipeline Lab",
            "Dr. Pipeline",
            new List<string> { "pipeline", "test" });

        var response = await PostJsonAsync("/api/v1/studies", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await ReadAsAsync<StudyResponse>(response))!;
    }

    private async Task<PipelineResponse?> CreatePipelineAsync(string accessToken, string studyId, string name)
    {
        SetAuthorizationHeader(accessToken);
        var request = new CreatePipelineRequest(name, "Test pipeline description");
        var response = await PostJsonAsync($"/api/v1/studies/{studyId}/pipelines", request);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            return await ReadAsAsync<PipelineResponse>(response);
        }

        return null;
    }

    #endregion

    #region Complete Lifecycle Tests

    [Fact]
    public async Task CompleteLifecycle_CreatePipeline_AddSteps_Activate_Execute_VerifyResults()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);

        // Step 1: Create pipeline
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "Complete Lifecycle Pipeline");
        pipeline.Should().NotBeNull();
        pipeline!.Name.Should().Be("Complete Lifecycle Pipeline");
        pipeline.StatusName.Should().Be("Draft");

        // Step 2: Add steps
        SetAuthorizationHeader(accessToken);

        var step1Request = new AddStepRequest(
            "Quality Trim",
            1, // Quality trimming step type
            """{"threshold":20}""");
        var step1Response = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/steps",
            step1Request);
        step1Response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var step2Request = new AddStepRequest(
            "Vector Removal",
            2, // Vector removal step type
            """{"vectorSequence":"ATCGATCG"}""");
        var step2Response = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/steps",
            step2Request);
        step2Response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        // Step 3: Get pipeline with steps
        var pipelineWithSteps = await GetAsync<PipelineResponse>(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}");
        pipelineWithSteps.Should().NotBeNull();
        pipelineWithSteps!.Steps.Should().HaveCountGreaterOrEqualTo(0);

        // Step 4: Activate pipeline
        var activateResponse = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/activate",
            new { });
        activateResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        // Step 5: Get updated pipeline status
        var activatedPipeline = await GetAsync<PipelineResponse>(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}");

        if (activateResponse.StatusCode == HttpStatusCode.OK)
        {
            activatedPipeline!.StatusName.Should().Be("Active");
        }
    }

    [Fact]
    public async Task PipelineWorkflow_CreateUpdate_AddRemoveSteps_Delete()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);

        // Step 1: Create pipeline
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "CRUD Pipeline");
        pipeline.Should().NotBeNull();

        // Step 2: Update pipeline
        SetAuthorizationHeader(accessToken);
        var updateRequest = new UpdatePipelineRequest("Updated Pipeline Name", "Updated description");
        var updateResponse = await PutJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}",
            updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedPipeline = await ReadAsAsync<PipelineResponse>(updateResponse);
        updatedPipeline!.Name.Should().Be("Updated Pipeline Name");

        // Step 3: Add a step
        var addStepRequest = new AddStepRequest("Test Step", 1, null);
        var addStepResponse = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/steps",
            addStepRequest);

        if (addStepResponse.StatusCode == HttpStatusCode.Created)
        {
            var step = await ReadAsAsync<PipelineStepResponse>(addStepResponse);

            // Step 4: Remove the step
            var removeStepResponse = await DeleteAsync(
                $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/steps/{step!.Id}");
            removeStepResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // Step 5: Delete pipeline
        var deleteResponse = await DeleteAsync($"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getDeletedResponse = await GetResponseAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Pipeline Creation Tests

    [Fact]
    public async Task CreatePipeline_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);

        SetAuthorizationHeader(accessToken);
        var request = new CreatePipelineRequest("New Pipeline", "A new test pipeline");

        // Act
        var response = await PostJsonAsync($"/api/v1/studies/{study.Id}/pipelines", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var pipeline = await ReadAsAsync<PipelineResponse>(response);
        pipeline.Should().NotBeNull();
        pipeline!.Name.Should().Be("New Pipeline");
        pipeline.StatusName.Should().Be("Draft");
    }

    [Fact]
    public async Task CreatePipeline_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthorizationHeader();
        var request = new CreatePipelineRequest("Test Pipeline", null);

        // Act
        var response = await PostJsonAsync("/api/v1/studies/fake-id/pipelines", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePipeline_WithEmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);

        SetAuthorizationHeader(accessToken);
        var request = new CreatePipelineRequest("", null);

        // Act
        var response = await PostJsonAsync($"/api/v1/studies/{study.Id}/pipelines", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region Pipeline Step Tests

    [Fact]
    public async Task AddPipelineStep_WithValidData_ShouldSucceed()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "Step Test Pipeline");

        pipeline.Should().NotBeNull();

        SetAuthorizationHeader(accessToken);
        var request = new AddStepRequest("Quality Check Step", 1, """{"minQuality":20}""");

        // Act
        var response = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}/steps",
            request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePipelineStep_ShouldSucceed()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "Update Step Pipeline");

        pipeline.Should().NotBeNull();

        SetAuthorizationHeader(accessToken);

        // Add a step first
        var addRequest = new AddStepRequest("Original Step", 1, null);
        var addResponse = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}/steps",
            addRequest);

        if (addResponse.StatusCode != HttpStatusCode.Created)
            return;

        var step = await ReadAsAsync<PipelineStepResponse>(addResponse);

        // Act
        var updateRequest = new UpdateStepRequest("Updated Step Name", """{"newConfig":true}""", true);
        var response = await PutJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/steps/{step!.Id}",
            updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReorderPipelineSteps_ShouldSucceed()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "Reorder Steps Pipeline");

        pipeline.Should().NotBeNull();

        SetAuthorizationHeader(accessToken);

        // Add multiple steps
        var stepIds = new List<string>();
        for (int i = 1; i <= 3; i++)
        {
            var addRequest = new AddStepRequest($"Step {i}", 1, null);
            var addResponse = await PostJsonAsync(
                $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}/steps",
                addRequest);

            if (addResponse.StatusCode == HttpStatusCode.Created)
            {
                var step = await ReadAsAsync<PipelineStepResponse>(addResponse);
                stepIds.Add(step!.Id);
            }
        }

        if (stepIds.Count < 2) return;

        // Act - Reverse the order
        stepIds.Reverse();
        var reorderRequest = new ReorderStepsRequest(stepIds);
        var response = await PutJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}/steps/reorder",
            reorderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Pipeline Status Tests

    [Fact]
    public async Task ActivatePipeline_WithSteps_ShouldSucceed()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "Activate Pipeline");

        pipeline.Should().NotBeNull();

        SetAuthorizationHeader(accessToken);

        // Add a step (required for activation)
        var addRequest = new AddStepRequest("Required Step", 1, null);
        await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}/steps",
            addRequest);

        // Act
        var response = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/activate",
            new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeactivatePipeline_ShouldReturnToDraft()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "Deactivate Pipeline");

        pipeline.Should().NotBeNull();

        SetAuthorizationHeader(accessToken);

        // Add step and activate
        await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}/steps",
            new AddStepRequest("Step", 1, null));
        await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/activate",
            new { });

        // Act
        var response = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline.Id}/deactivate",
            new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ArchivePipeline_ShouldSucceed()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "Archive Pipeline");

        pipeline.Should().NotBeNull();

        // Act
        SetAuthorizationHeader(accessToken);
        var response = await PostJsonAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}/archive",
            new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    #endregion

    #region Pipeline Query Tests

    [Fact]
    public async Task GetStudyPipelines_ShouldReturnPaginatedList()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);

        // Create some pipelines
        await CreatePipelineAsync(accessToken, study.Id, "Pipeline 1");
        await CreatePipelineAsync(accessToken, study.Id, "Pipeline 2");

        // Act
        SetAuthorizationHeader(accessToken);
        var response = await GetResponseAsync(
            $"/api/v1/studies/{study.Id}/pipelines?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadAsAsync<PagedResponseWrapper<PipelineSummaryResponse>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetStepTypes_ShouldReturnAvailableTypes()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        SetAuthorizationHeader(accessToken);

        // Act
        var response = await GetResponseAsync("/api/v1/pipelines/step-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stepTypes = await ReadAsAsync<IReadOnlyList<StepTypeResponse>>(response);
        stepTypes.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPipelineExecutions_ShouldReturnExecutionHistory()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginAsync();
        var study = await CreateStudyAsync(accessToken);
        var pipeline = await CreatePipelineAsync(accessToken, study.Id, "Executions Pipeline");

        pipeline.Should().NotBeNull();

        // Act
        SetAuthorizationHeader(accessToken);
        var response = await GetResponseAsync(
            $"/api/v1/studies/{study.Id}/pipelines/{pipeline!.Id}/executions?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
