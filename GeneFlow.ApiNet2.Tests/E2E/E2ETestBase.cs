using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GeneFlow.ApiNet2.API;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;

namespace GeneFlow.ApiNet2.Tests.E2E;

/// <summary>
/// Base class for E2E tests that use real database containers and full application stack.
/// </summary>
[Collection(E2ETestCollection.Name)]
public abstract class E2ETestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainerFixture PostgresFixture;
    protected readonly RedisContainerFixture RedisFixture;
    protected HttpClient Client = null!;
    protected WebApplicationFactory<Program> Factory = null!;
    private Respawner? _respawner;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected E2ETestBase(
        PostgreSqlContainerFixture postgresFixture,
        RedisContainerFixture redisFixture)
    {
        PostgresFixture = postgresFixture;
        RedisFixture = redisFixture;
    }

    public virtual async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureTestServices(services =>
                {
                    ConfigureTestServices(services);
                });
            });

        Client = Factory.CreateClient();

        // Initialize Respawner for database cleanup using DbConnection for PostgreSQL
        await using var connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" }
            });
    }

    public virtual async Task DisposeAsync()
    {
        // Reset database to clean state
        if (_respawner != null)
        {
            await using var connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }

        Client.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Override to configure test services.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
        // Subclasses can override to add custom test configuration
    }

    /// <summary>
    /// Resets the database to a clean state.
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        if (_respawner != null)
        {
            await _respawner.ResetAsync(PostgresFixture.ConnectionString);
        }
    }

    #region HTTP Helper Methods

    /// <summary>
    /// Sets the authorization header with a JWT token.
    /// </summary>
    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clears the authorization header.
    /// </summary>
    protected void ClearAuthorizationHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Posts JSON content and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string url, T content)
    {
        return await Client.PostAsJsonAsync(url, content, JsonOptions);
    }

    /// <summary>
    /// Puts JSON content and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string url, T content)
    {
        return await Client.PutAsJsonAsync(url, content, JsonOptions);
    }

    /// <summary>
    /// Patches JSON content and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> PatchJsonAsync<T>(string url, T content)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(content, options: JsonOptions)
        };
        return await Client.SendAsync(request);
    }

    /// <summary>
    /// Gets and deserializes a response.
    /// </summary>
    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    /// <summary>
    /// Gets a response without deserialization.
    /// </summary>
    protected async Task<HttpResponseMessage> GetResponseAsync(string url)
    {
        return await Client.GetAsync(url);
    }

    /// <summary>
    /// Deletes a resource and returns the response.
    /// </summary>
    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    /// <summary>
    /// Reads the response content as a specific type.
    /// </summary>
    protected async Task<T?> ReadAsAsync<T>(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    #endregion

    #region Test Data Helpers

    /// <summary>
    /// Generates a unique email for testing.
    /// </summary>
    protected static string GenerateTestEmail() => $"test_{Guid.NewGuid():N}@example.com";

    /// <summary>
    /// Generates a unique username for testing.
    /// </summary>
    protected static string GenerateTestUsername() => $"user_{Guid.NewGuid():N}"[..20];

    /// <summary>
    /// Default test password that meets complexity requirements.
    /// </summary>
    protected const string TestPassword = "TestPassword123!";

    #endregion
}

/// <summary>
/// API error response model.
/// </summary>
public sealed record ApiErrorResponse(string Code, string Message);

/// <summary>
/// Paged response wrapper for E2E tests.
/// </summary>
public sealed record PagedResponseWrapper<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
