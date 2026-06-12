using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace GeneFlow.ApiNet2.Tests.Common.Fixtures;

/// <summary>
/// Creates the tables for EF Core contexts in a shared PostgreSQL container.
/// <see cref="Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator.EnsureCreatedAsync"/>
/// is a no-op when <em>any</em> table already exists in the database, so when
/// multiple contexts/fixtures share one container the second context's tables
/// are never created. This helper generates the CREATE script for the supplied
/// context and executes it statement-by-statement, swallowing PostgreSQL
/// duplicate-object errors (schemas, tables, indexes, constraints, sequences)
/// so re-runs and cross-fixture overlap are safe.
/// </summary>
public static class PostgresSchemaInitializer
{
    /// <summary>
    /// Creates the schema objects for <paramref name="context"/>, ignoring
    /// objects that already exist in the target database.
    /// </summary>
    public static async Task EnsureContextSchemaAsync(DbContext context)
    {
        var creator = context.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
        var script = creator.GenerateCreateScript();

        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
            await connection.OpenAsync();
        try
        {
            foreach (var statement in SplitPostgresStatements(script))
            {
                if (string.IsNullOrWhiteSpace(statement))
                    continue;

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
            if (wasClosed)
                await connection.CloseAsync();
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
