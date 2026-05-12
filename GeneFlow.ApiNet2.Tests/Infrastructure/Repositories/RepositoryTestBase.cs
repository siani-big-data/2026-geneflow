using GeneFlow.ApiNet2.Tests.Common.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Repositories;

/// <summary>
/// Base class for repository integration tests that use real PostgreSQL containers.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class RepositoryTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainerFixture PostgresFixture;
    protected readonly RedisContainerFixture RedisFixture;
    private Respawner? _respawner;
    private bool _schemasCreated;

    protected RepositoryTestBase(
        PostgreSqlContainerFixture postgresFixture,
        RedisContainerFixture redisFixture)
    {
        PostgresFixture = postgresFixture;
        RedisFixture = redisFixture;
    }

    public virtual async Task InitializeAsync()
    {
        // Create schemas if not already created
        if (!_schemasCreated)
        {
            await CreateSchemasAsync();
            _schemasCreated = true;
        }

        // Initialize Respawner for database cleanup
        _respawner = await Respawner.CreateAsync(
            PostgresFixture.ConnectionString,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["identity", "studies", "traces", "pipelines", "public"]
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

    private async Task CreateSchemasAsync()
    {
        await using var connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE SCHEMA IF NOT EXISTS identity;
            CREATE SCHEMA IF NOT EXISTS studies;
            CREATE SCHEMA IF NOT EXISTS traces;
            CREATE SCHEMA IF NOT EXISTS pipelines;
            """;
        await command.ExecuteNonQueryAsync();
    }

    protected DbContextOptions<TContext> CreateDbContextOptions<TContext>() where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(PostgresFixture.ConnectionString)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;
    }

    protected ILogger<T> CreateLogger<T>()
    {
        return new LoggerFactory().CreateLogger<T>();
    }
}
