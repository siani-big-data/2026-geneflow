using GeneFlow.ApiNet2.Tests.Common.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace GeneFlow.ApiNet2.Tests.Common.Base;

/// <summary>
/// Base class for integration tests that use real database containers.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainerFixture PostgresFixture;
    protected readonly RedisContainerFixture RedisFixture;
    private Respawner? _respawner;

    protected IntegrationTestBase(
        PostgreSqlContainerFixture postgresFixture,
        RedisContainerFixture redisFixture)
    {
        PostgresFixture = postgresFixture;
        RedisFixture = redisFixture;
    }

    public virtual async Task InitializeAsync()
    {
        // Initialize Respawner for database cleanup
        _respawner = await Respawner.CreateAsync(
            PostgresFixture.ConnectionString,
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
            await _respawner.ResetAsync(PostgresFixture.ConnectionString);
        }
    }

    protected async Task ResetDatabaseAsync()
    {
        if (_respawner != null)
        {
            await _respawner.ResetAsync(PostgresFixture.ConnectionString);
        }
    }
}
