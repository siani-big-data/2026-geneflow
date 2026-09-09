using System.Net;
using System.Net.Http.Json;
using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

namespace GeneFlow.ApiNet2.Tests.API.Studies;

/// <summary>
/// Integration tests for study endpoints.
/// </summary>
public class StudyEndpointsTests : IClassFixture<GeneFlowWebApplicationFactory>, IDisposable
{
    private readonly GeneFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    public StudyEndpointsTests(GeneFlowWebApplicationFactory factory)
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

    #region GET /api/v1/studies/research-fields (Public)

    [Fact]
    public async Task GetResearchFields_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/research-fields");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetResearchFields_ShouldReturnListOfResearchFields()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/research-fields");
        var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<ResearchFieldResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
    }

    #endregion

    #region GET /api/v1/studies/public (Public)

    [Fact]
    public async Task GetPublicStudies_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/public");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPublicStudies_WithPagination_ShouldReturnPagedResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/public?pageNumber=1&pageSize=10");
        var content = await response.Content.ReadFromJsonAsync<PagedResponse<StudySummaryResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.PageNumber.Should().BeGreaterThanOrEqualTo(1);
        content.PageSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPublicStudies_WithSearchTerm_ShouldReturnFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/public?searchTerm=genomics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPublicStudies_WithResearchFieldFilter_ShouldReturnFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/public?researchFieldId=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPublicStudies_WithTags_ShouldReturnFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/public?tags=genetics,dna");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/v1/studies/featured (Public)

    [Fact]
    public async Task GetFeaturedStudies_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/featured");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFeaturedStudies_WithLimit_ShouldReturnLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/featured?limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/v1/studies/mine (Authenticated)

    [Fact]
    public async Task GetUserStudies_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/mine");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserStudies_WithAuthentication_ShouldReturnOk()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/v1/studies/mine?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserStudies_WithFilters_ShouldReturnFilteredResults()
    {
        // Act
        var response = await _authenticatedClient.GetAsync(
            "/api/v1/studies/mine?pageNumber=1&pageSize=10&statusId=2&searchTerm=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/v1/studies/{studyId} (Authenticated)

    [Fact]
    public async Task GetStudyById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/studies/S00000001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStudyById_WithValidId_ShouldReturnOk()
    {
        // Arrange
        _factory.SetupStudyExists("S00000001");

        // Act
        var response = await _authenticatedClient.GetAsync("/api/v1/studies/S00000001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStudyById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupStudyNotFound("S99999999");

        // Act
        var response = await _authenticatedClient.GetAsync("/api/v1/studies/S99999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/v1/studies (Authenticated)

    [Fact]
    public async Task CreateStudy_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateStudyRequest
        {
            Title = "Test Study",
            ResearchFieldId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/studies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateStudy_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateStudyRequest
        {
            Title = "Test Study for Genomics Research",
            Description = "A comprehensive study of genomic sequences",
            ResearchFieldId = 1,
            Institution = "Test University",
            PrincipalInvestigator = "Dr. Jane Doe",
            Tags = new List<string> { "genomics", "research" }
        };

        _factory.SetupCreateStudySuccess();

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/v1/studies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateStudy_WithInvalidTitle_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateStudyRequest
        {
            Title = "Ab", // Too short (min 5 chars)
            ResearchFieldId = 1
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/v1/studies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateStudy_WithMissingRequiredFields_ShouldReturnBadRequest()
    {
        // Arrange - Missing Title and ResearchFieldId
        var request = new { Description = "Only description" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/v1/studies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT /api/v1/studies/{studyId} (Authenticated)

    [Fact]
    public async Task UpdateStudy_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateStudyRequest
        {
            Title = "Updated Study Title",
            ResearchFieldId = 1
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/studies/S00000001", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStudy_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new UpdateStudyRequest
        {
            Title = "Updated Study Title",
            Description = "Updated description",
            ResearchFieldId = 1,
            Institution = "Updated University",
            PrincipalInvestigator = "Dr. John Smith",
            Tags = new List<string> { "updated", "tags" }
        };

        _factory.SetupUpdateStudySuccess("S00000001");

        // Act
        var response = await _authenticatedClient.PutAsJsonAsync("/api/v1/studies/S00000001", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStudy_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var request = new UpdateStudyRequest
        {
            Title = "Updated Study Title",
            ResearchFieldId = 1
        };

        _factory.SetupStudyNotFound("S99999999");

        // Act
        var response = await _authenticatedClient.PutAsJsonAsync("/api/v1/studies/S99999999", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PATCH /api/v1/studies/{studyId}/status (Authenticated)

    [Fact]
    public async Task ChangeStudyStatus_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new ChangeStudyStatusRequest { NewStatusId = 2 };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/v1/studies/S00000001/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeStudyStatus_WithValidStatus_ShouldReturnOk()
    {
        // Arrange
        var request = new ChangeStudyStatusRequest { NewStatusId = 2 }; // Active

        _factory.SetupChangeStatusSuccess("S00000001");

        // Act
        var response = await _authenticatedClient.PatchAsJsonAsync("/api/v1/studies/S00000001/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangeStudyStatus_WithInvalidStatus_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new ChangeStudyStatusRequest { NewStatusId = 99 }; // Invalid

        // Act
        var response = await _authenticatedClient.PatchAsJsonAsync("/api/v1/studies/S00000001/status", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE /api/v1/studies/{studyId} (Authenticated)

    [Fact]
    public async Task DeleteStudy_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync("/api/v1/studies/S00000001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteStudy_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        _factory.SetupDeleteStudySuccess("S00000001");

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/v1/studies/S00000001");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteStudy_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupStudyNotFound("S99999999");

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/v1/studies/S99999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/v1/studies/{studyId}/duplicate (Authenticated)

    [Fact]
    public async Task DuplicateStudy_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/studies/S00000001/duplicate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DuplicateStudy_WithValidId_ShouldReturnCreated()
    {
        // Arrange
        _factory.SetupDuplicateStudySuccess("S00000001");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/v1/studies/S00000001/duplicate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task DuplicateStudy_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupStudyNotFound("S99999999");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/v1/studies/S99999999/duplicate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
