using GeneFlow.ApiNet2.Tests.Common.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

        // Hook: subclasses can apply EF Core migrations or call EnsureCreatedAsync()
        // here so Respawn finds real tables before it builds the deletion graph.
        await PrepareSchemaAsync();

        // Initialize Respawner for database cleanup. Respawn's string overload
        // only works with SqlConnection, so we must hand it an open NpgsqlConnection.
        await using var connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["identity", "studies", "traces", "pipelines", "activity", "public"]
            });
    }

    /// <summary>
    /// Subclasses override this to create tables (via EF migrations or
    /// <c>EnsureCreatedAsync</c>) before Respawn captures the deletion graph.
    /// </summary>
    protected virtual Task PrepareSchemaAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        if (_respawner != null)
        {
            await using var connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }
    }

    protected async Task ResetDatabaseAsync()
    {
        if (_respawner != null)
        {
            await using var connection = new NpgsqlConnection(PostgresFixture.ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
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
            CREATE SCHEMA IF NOT EXISTS activity;
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

    /// <summary>
    /// Creates the tables for <typeparamref name="TContext"/> in the shared
    /// PostgreSQL container. See <see cref="PostgresSchemaInitializer"/> for
    /// why <see cref="DatabaseFacade.EnsureCreatedAsync"/> is not enough.
    /// </summary>
    protected static Task EnsureContextSchemaAsync<TContext>(TContext context)
        where TContext : DbContext
        => PostgresSchemaInitializer.EnsureContextSchemaAsync(context);
}
