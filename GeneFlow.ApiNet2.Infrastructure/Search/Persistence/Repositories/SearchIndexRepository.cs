using System.Text;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Entities;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Search.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GeneFlow.ApiNet2.Infrastructure.Search.Persistence.Repositories;

/// <summary>
/// Repository backed by the <c>search.search_index</c> table and its
/// server-generated <c>tsv</c> tsvector column.
/// </summary>
public sealed class SearchIndexRepository : ISearchIndexRepository
{
    private readonly SearchContext _context;

    public SearchIndexRepository(SearchContext context)
    {
        _context = context;
    }

    public Task<SearchIndexEntry?> GetAsync(
        SearchObjectType objectType, string objectId, CancellationToken cancellationToken = default)
    {
        return _context.SearchIndex
            .FirstOrDefaultAsync(
                e => e.ObjectType == objectType && e.ObjectId == objectId,
                cancellationToken);
    }

    public async Task AddAsync(SearchIndexEntry entry, CancellationToken cancellationToken = default)
        => await _context.SearchIndex.AddAsync(entry, cancellationToken);

    public void Update(SearchIndexEntry entry) => _context.SearchIndex.Update(entry);

    public void Remove(SearchIndexEntry entry) => _context.SearchIndex.Remove(entry);

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        SearchObjectType? type,
        string? ownerId,
        string? tag,
        DateTime? cursorUpdatedBefore,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build a prefix-friendly ts_query: tokenize the user input, strip
        // anything that's not alphanumeric (avoids breaking to_tsquery with
        // operators like &/|/!/:), and append ':*' to each token so partial
        // matches work. Empty input → no hits.
        var tsquery = BuildPrefixTsQuery(query);
        if (tsquery.Length == 0)
        {
            return Array.Empty<SearchHit>();
        }

        var sql = new StringBuilder();
        sql.Append(@"
SELECT id, object_type, object_id, owner_id, title, body, tags,
       is_public, created_at, updated_at,
       ts_rank_cd(tsv, to_tsquery('simple', @q)) AS rank
FROM search.search_index
WHERE tsv @@ to_tsquery('simple', @q)");

        var parameters = new List<NpgsqlParameter>
        {
            new("@q", tsquery),
            new("@limit", pageSize),
        };

        if (type is not null)
        {
            sql.Append(" AND object_type = @type");
            parameters.Add(new NpgsqlParameter("@type", type.Id));
        }
        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            sql.Append(" AND owner_id = @owner");
            parameters.Add(new NpgsqlParameter("@owner", ownerId));
        }
        if (!string.IsNullOrWhiteSpace(tag))
        {
            sql.Append(" AND tags ILIKE @tag");
            parameters.Add(new NpgsqlParameter("@tag", "%" + tag + "%"));
        }
        if (cursorUpdatedBefore.HasValue && cursorId.HasValue)
        {
            sql.Append(" AND (updated_at, id) < (@cursorTs, @cursorId)");
            parameters.Add(new NpgsqlParameter("@cursorTs", cursorUpdatedBefore.Value));
            parameters.Add(new NpgsqlParameter("@cursorId", cursorId.Value));
        }

        sql.Append(" ORDER BY rank DESC, updated_at DESC, id DESC LIMIT @limit");

        return await ExecuteHitQueryAsync(sql.ToString(), parameters, cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHit>> GetRecentPublicAsync(
        SearchObjectType? type,
        DateTime? cursorUpdatedBefore,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var sql = new StringBuilder();
        sql.Append(@"
SELECT id, object_type, object_id, owner_id, title, body, tags,
       is_public, created_at, updated_at, 0::float AS rank
FROM search.search_index
WHERE is_public = TRUE");

        var parameters = new List<NpgsqlParameter>
        {
            new("@limit", pageSize),
        };

        if (type is not null)
        {
            sql.Append(" AND object_type = @type");
            parameters.Add(new NpgsqlParameter("@type", type.Id));
        }
        if (cursorUpdatedBefore.HasValue && cursorId.HasValue)
        {
            sql.Append(" AND (updated_at, id) < (@cursorTs, @cursorId)");
            parameters.Add(new NpgsqlParameter("@cursorTs", cursorUpdatedBefore.Value));
            parameters.Add(new NpgsqlParameter("@cursorId", cursorId.Value));
        }

        sql.Append(" ORDER BY updated_at DESC, id DESC LIMIT @limit");

        return await ExecuteHitQueryAsync(sql.ToString(), parameters, cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHit>> GetTrendingPublicAsync(
        SearchObjectType? type,
        int limit,
        CancellationToken cancellationToken = default)
    {
        // v1: weight by recency only (newer = trendier). A real trending
        // score can incorporate stars / view counts in a follow-up.
        var sql = new StringBuilder();
        sql.Append(@"
SELECT id, object_type, object_id, owner_id, title, body, tags,
       is_public, created_at, updated_at,
       EXTRACT(EPOCH FROM (NOW() - updated_at)) AS rank
FROM search.search_index
WHERE is_public = TRUE");

        var parameters = new List<NpgsqlParameter>
        {
            new("@limit", limit),
        };

        if (type is not null)
        {
            sql.Append(" AND object_type = @type");
            parameters.Add(new NpgsqlParameter("@type", type.Id));
        }

        sql.Append(" ORDER BY rank ASC LIMIT @limit");

        return await ExecuteHitQueryAsync(sql.ToString(), parameters, cancellationToken);
    }

    public async Task<IReadOnlyList<SearchHit>> GetFeedAsync(
        IReadOnlyCollection<string> followedOwnerIds,
        IReadOnlyCollection<string> watchedStudyIds,
        string selfOwnerId,
        DateTime? cursorUpdatedBefore,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var sql = new StringBuilder();
        sql.Append(@"
SELECT id, object_type, object_id, owner_id, title, body, tags,
       is_public, created_at, updated_at, 0::float AS rank
FROM search.search_index
WHERE (
    owner_id = @self
    OR owner_id = ANY(@followed)
    OR (object_type = @studyType AND object_id = ANY(@watched))
)");

        var parameters = new List<NpgsqlParameter>
        {
            new("@self", selfOwnerId),
            new("@followed", followedOwnerIds.ToArray()),
            new("@watched", watchedStudyIds.ToArray()),
            new("@studyType", SearchObjectType.Study.Id),
            new("@limit", pageSize),
        };

        if (cursorUpdatedBefore.HasValue && cursorId.HasValue)
        {
            sql.Append(" AND (updated_at, id) < (@cursorTs, @cursorId)");
            parameters.Add(new NpgsqlParameter("@cursorTs", cursorUpdatedBefore.Value));
            parameters.Add(new NpgsqlParameter("@cursorId", cursorId.Value));
        }

        sql.Append(" ORDER BY updated_at DESC, id DESC LIMIT @limit");

        return await ExecuteHitQueryAsync(sql.ToString(), parameters, cancellationToken);
    }

    /// <summary>
    /// Tokenizes a free-form search string into a Postgres ts_query that
    /// matches token prefixes (so "stud" matches "study"). Drops every
    /// character that is not a letter or digit to avoid syntax errors in
    /// to_tsquery.
    /// </summary>
    private static string BuildPrefixTsQuery(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var tokens = raw
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => new string(token.Where(char.IsLetterOrDigit).ToArray()))
            .Where(t => t.Length > 0)
            .Select(t => t + ":*");

        return string.Join(" & ", tokens);
    }

    private async Task<IReadOnlyList<SearchHit>> ExecuteHitQueryAsync(
        string sql,
        List<NpgsqlParameter> parameters,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var p in parameters) command.Parameters.Add(p);

            var hits = new List<SearchHit>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var objectTypeId = reader.GetInt32(1);
                var objectId = reader.GetString(2);
                var ownerId = reader.IsDBNull(3) ? null : reader.GetString(3);
                var title = reader.GetString(4);
                var body = reader.IsDBNull(5) ? null : reader.GetString(5);
                var tags = reader.IsDBNull(6) ? null : reader.GetString(6);
                var isPublic = reader.GetBoolean(7);
                var updatedAt = reader.GetDateTime(9);
                var rank = reader.IsDBNull(10) ? 0d : Convert.ToDouble(reader.GetValue(10));

                hits.Add(new SearchHit(
                    id, SearchObjectType.FromId(objectTypeId)!, objectId, ownerId,
                    title, body, tags, isPublic, DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc), rank));
            }

            return hits;
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }
}
