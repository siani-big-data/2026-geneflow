using System.Net;
using System.Net.Http.Json;
using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;

namespace GeneFlow.ApiNet2.Tests.API.Studies;

/// <summary>
/// Integration tests for study paper endpoints.
/// </summary>
public class StudyPaperEndpointsTests : IClassFixture<GeneFlowWebApplicationFactory>, IDisposable
{
    private readonly GeneFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    private const string TestStudyId = "S00000001";
    // StudyPaperId is prefixed with 'R' (study_papers sequence); see StudyPaperId.Prefix.
    private const string TestPaperId = "R00000001";

    public StudyPaperEndpointsTests(GeneFlowWebApplicationFactory factory)
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

    #region GET /api/v1/studies/{studyId}/papers

    [Fact]
    public async Task GetStudyPapers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/studies/{TestStudyId}/papers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStudyPapers_WithValidStudyId_ShouldReturnOk()
    {
        // Arrange
        _factory.SetupGetPapersSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/v1/studies/{TestStudyId}/papers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStudyPapers_WithValidStudyId_ShouldReturnPaperList()
    {
        // Arrange
        _factory.SetupGetPapersSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/v1/studies/{TestStudyId}/papers");
        var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<StudyPaperResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStudyPapers_WithNonExistentStudy_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupStudyNotFound("S99999999");

        // Act
        var response = await _authenticatedClient.GetAsync("/api/v1/studies/S99999999/papers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/v1/studies/{studyId}/papers

    [Fact]
    public async Task AddStudyPaper_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new AddStudyPaperRequest
        {
            Title = "Research Paper on Genomics"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/papers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddStudyPaper_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new AddStudyPaperRequest
        {
            Title = "Research Paper on Genomics",
            Authors = "Dr. Jane Doe, Dr. John Smith",
            Doi = "10.1234/example.doi",
            Abstract = "This paper presents findings on genomic research...",
            Journal = "Nature Genetics",
            PublicationYear = 2024
        };

        _factory.SetupAddPaperSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/papers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task AddStudyPaper_WithMinimalData_ShouldReturnCreated()
    {
        // Arrange - Only required field (Title)
        var request = new AddStudyPaperRequest
        {
            Title = "Minimal Paper Title"
        };

        _factory.SetupAddPaperSuccess(TestStudyId);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/papers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddStudyPaper_WithMissingTitle_ShouldReturnBadRequest()
    {
        // Arrange - Missing required Title field
        var request = new { Authors = "Dr. Jane Doe" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/papers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddStudyPaper_WithInvalidPublicationYear_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AddStudyPaperRequest
        {
            Title = "Research Paper",
            PublicationYear = 1800 // Invalid (min 1900)
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/v1/studies/{TestStudyId}/papers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddStudyPaper_WithNonExistentStudy_ShouldReturnNotFound()
    {
        // Arrange
        var request = new AddStudyPaperRequest
        {
            Title = "Research Paper"
        };

        _factory.SetupStudyNotFound("S99999999");

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/v1/studies/S99999999/papers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/v1/studies/{studyId}/papers/{paperId}

    [Fact]
    public async Task RemoveStudyPaper_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/v1/studies/{TestStudyId}/papers/{TestPaperId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveStudyPaper_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        _factory.SetupRemovePaperSuccess(TestStudyId, TestPaperId);

        // Act
        var response = await _authenticatedClient.DeleteAsync($"/api/v1/studies/{TestStudyId}/papers/{TestPaperId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveStudyPaper_WithNonExistentPaper_ShouldReturnNotFound()
    {
        // Arrange
        _factory.SetupPaperNotFound(TestStudyId, "R99999999");

        // Act
        var response = await _authenticatedClient.DeleteAsync($"/api/v1/studies/{TestStudyId}/papers/R99999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
