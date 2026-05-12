using System.IO.Compression;
using System.Net;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Tests.API;
using DomainUserId = GeneFlow.ApiNet2.Domain.Identity.UserId;
using DomainStudyTitle = GeneFlow.ApiNet2.Domain.Studies.ValueObjects.StudyTitle;
using DomainStudyDescription = GeneFlow.ApiNet2.Domain.Studies.ValueObjects.StudyDescription;
using DomainResearchField = GeneFlow.ApiNet2.Domain.Studies.Enumerations.ResearchField;

namespace GeneFlow.ApiNet2.Tests.API.Studies;

public class ExportStudyEndpointTests : IClassFixture<GeneFlowWebApplicationFactory>, IDisposable
{
    private readonly GeneFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly HttpClient _authenticatedClient;

    private const string TestStudyId = "S00000001";

    public ExportStudyEndpointTests(GeneFlowWebApplicationFactory factory)
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
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExportStudy_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync($"/api/v1/studies/{TestStudyId}/export");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportStudy_WhenStudyNotFound_ShouldReturnNotFound()
    {
        _factory.SetupStudyNotFound(TestStudyId);

        var response = await _authenticatedClient.GetAsync($"/api/v1/studies/{TestStudyId}/export");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportStudy_WhenUserIsNotMember_ShouldReturnNotFound()
    {
        // SetupStudyExists creates the study with ownerId = UserId(1) (= U00000001).
        // To simulate "not a member", build a study whose owner is a different user
        // and override the GetByIdWithMembersAsync returned aggregate.
        var foreignOwnerStudy = BuildStudyOwnedBy(new DomainUserId(99), TestStudyId);
        _factory.MockStudyRepository
            .GetByIdAsync(Arg.Is<StudyId>(id => id.ToString() == TestStudyId), Arg.Any<CancellationToken>())
            .Returns(foreignOwnerStudy);
        _factory.MockStudyRepository
            .GetByIdWithMembersAsync(Arg.Is<StudyId>(id => id.ToString() == TestStudyId), Arg.Any<CancellationToken>())
            .Returns(foreignOwnerStudy);

        var response = await _authenticatedClient.GetAsync($"/api/v1/studies/{TestStudyId}/export");

        // StudyErrors.NotAMember is mapped to NotFound by ResultExtensions.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportStudy_WhenMember_ShouldReturnZipWithExpectedEntries()
    {
        _factory.SetupExportStudySuccess(TestStudyId);
        // No traces, no papers — minimal valid export.

        var response = await _authenticatedClient.GetAsync($"/api/v1/studies/{TestStudyId}/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");
        response.Content.Headers.ContentDisposition?.DispositionType.Should().Be("attachment");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entries = archive.Entries.Select(e => e.FullName).ToList();
        entries.Should().Contain("study.json");
        entries.Should().Contain("members.json");
        entries.Should().Contain("traces/traces.json");
        entries.Should().Contain("papers/papers.json");
        entries.Should().Contain("README.txt");
    }

    private static Study BuildStudyOwnedBy(DomainUserId ownerId, string studyId)
    {
        var numericPart = long.Parse(studyId.Substring(1));
        var id = new StudyId(numericPart);
        var title = DomainStudyTitle.Create("Foreign Study").Value;
        var description = DomainStudyDescription.Create("Desc").Value;
        return Study.Create(
            id,
            ownerId,
            title,
            description,
            DomainResearchField.Genomics).Value;
    }
}
