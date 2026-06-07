using System.Data;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
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
    /// PostgreSQL container. <see cref="DatabaseFacade.EnsureCreatedAsync"/>
    /// is a no-op when <em>any</em> table already exists in the database, so
    /// when multiple fixtures share one container the second context's tables
    /// are never created. This helper generates the CREATE script for the
    /// supplied context and executes it statement-by-statement, swallowing
    /// PostgreSQL duplicate-object errors (schemas, tables, indexes,
    /// constraints, sequences) so re-runs and cross-fixture overlap are safe.
    /// </summary>
    protected static async Task EnsureContextSchemaAsync<TContext>(TContext context)
        where TContext : DbContext
    {
        var creator = context.GetService<IRelationalDatabaseCreator>();
        var script = creator.GenerateCreateScript();

        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed) await connection.OpenAsync();
        try
        {
            foreach (var statement in SplitPostgresStatements(script))
            {
                if (string.IsNullOrWhiteSpace(statement)) continue;

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = statement;
                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (PostgresException ex) when (IsDuplicateObject(ex.SqlState))
                {
                    // Already created by a sibling fixture in this container.
                }
            }
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }

    /// <summary>
    /// Splits a PostgreSQL script on top-level <c>;</c> boundaries while
    /// respecting dollar-quoted string literals (e.g. EF emits
    /// <c>DO $EF$ BEGIN ... END $EF$;</c> blocks for idempotent schema
    /// creation, and a naive split would tear those blocks apart).
    /// </summary>
    private static IEnumerable<string> SplitPostgresStatements(string script)
    {
        var start = 0;
        string? dollarTag = null;
        for (var i = 0; i < script.Length; i++)
        {
            var c = script[i];

            if (dollarTag is null)
            {
                if (c == '$')
                {
                    // Look for an opening $tag$ — tag may be empty ($$).
                    var end = script.IndexOf('$', i + 1);
                    if (end >= 0)
                    {
                        var tag = script.Substring(i, end - i + 1);
                        // Tag body must be empty or an identifier (letters/digits/_).
                        var body = tag.Substring(1, tag.Length - 2);
                        if (body.All(ch => ch == '_' || char.IsLetterOrDigit(ch)))
                        {
                            dollarTag = tag;
                            i = end; // skip past the opening tag
                            continue;
                        }
                    }
                }

                if (c == ';')
                {
                    yield return script.Substring(start, i - start);
                    start = i + 1;
                }
            }
            else
            {
                // Inside a dollar-quoted block — look for the matching closing tag.
                if (c == '$' && string.CompareOrdinal(script, i, dollarTag, 0, dollarTag.Length) == 0)
                {
                    i += dollarTag.Length - 1;
                    dollarTag = null;
                }
            }
        }

        if (start < script.Length)
            yield return script.Substring(start);
    }

    private static bool IsDuplicateObject(string sqlState) => sqlState switch
    {
        "42P06" => true, // duplicate_schema
        "42P07" => true, // duplicate_table
        "42710" => true, // duplicate_object (indexes, constraints, sequences)
        _ => false,
    };
}
