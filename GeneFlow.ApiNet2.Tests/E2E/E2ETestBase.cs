using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GeneFlow.ApiNet2.API;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Infrastructure.Activity.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.PaymentMethods.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Pipelines.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Plans.Services;
using GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Search.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Subscriptions.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Every EF Core context registered by the application. The E2E host is
    /// pointed at the shared PostgreSQL container, so each context's tables
    /// must be created there before Respawn builds its deletion graph.
    /// </summary>
    private static readonly Type[] ContextTypes =
    [
        typeof(UserContext),
        typeof(StudyContext),
        typeof(OrgsContext),
        typeof(ProfileContext),
        typeof(PlanContext),
        typeof(SubscriptionContext),
        typeof(TraceContext),
        typeof(PipelineContext),
        typeof(DiscussionContext),
        typeof(NotificationContext),
        typeof(ActivityContext),
        typeof(SearchContext),
        typeof(PaymentMethodContext)
    ];

    private static readonly string[] Schemas =
    [
        "identity", "studies", "orgs", "profiles", "plans", "subscriptions",
        "traces", "pipelines", "discussions", "notifications", "activity",
        "search", "billing", "public"
    ];

    public virtual async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                // Point the whole application at the test containers instead of
                // the developer's local services from appsettings.json.
                builder.UseSetting("ConnectionStrings:DefaultConnection", PostgresFixture.ConnectionString);
                builder.UseSetting("Redis:ConnectionString", RedisFixture.ConnectionString);

                builder.ConfigureTestServices(services =>
                {
                    // Never send real email from E2E runs.
                    services.RemoveAll<IEmailService>();
                    services.AddScoped(_ => Substitute.For<IEmailService>());

                    ConfigureTestServices(services);
                });
            });

        Client = Factory.CreateClient();

        // Create every context's tables in the container so the API (and
        // Respawn below) sees a fully initialized database.
        using (var scope = Factory.Services.CreateScope())
        {
            foreach (var contextType in ContextTypes)
            {
                var context = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                await PostgresSchemaInitializer.EnsureContextSchemaAsync(context);
            }

            // The app skips migrations + seeding in the Testing environment
            // (see DatabaseExtensions), so seed the default plans here. The
            // registration flow needs them to create free subscriptions.
            var planSeeder = new PlanSeeder(
                scope.ServiceProvider.GetRequiredService<PlanContext>(),
                scope.ServiceProvider.GetRequiredService<ISequenceGenerator>(),
                scope.ServiceProvider.GetRequiredService<ILogger<PlanSeeder>>());
            await planSeeder.SeedAsync();
        }

        // Initialize Respawner for database cleanup using DbConnection for PostgreSQL
        await using var connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = Schemas
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
            await using var connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
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
    /// Force-confirms a user's email via the dev-only endpoint so the user
    /// can log in (login rejects unverified emails).
    /// </summary>
    protected async Task ConfirmEmailAsync(string email)
    {
        var response = await PostJsonAsync("/api/v1/auth/dev/confirm-email", new { Email = email });
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Generates a unique email for testing.
    /// </summary>
    protected static string GenerateTestEmail() => $"test_{Guid.NewGuid():N}@example.com";

    /// <summary>
    /// Generates a unique username for testing. Usernames must be
    /// alphanumeric only (see <c>UsernameValidator</c>).
    /// </summary>
    protected static string GenerateTestUsername() => $"user{Guid.NewGuid():N}"[..20];

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
