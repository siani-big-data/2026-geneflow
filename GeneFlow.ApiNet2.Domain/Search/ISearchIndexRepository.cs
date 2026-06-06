using GeneFlow.ApiNet2.Domain.Search.Entities;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;

namespace GeneFlow.ApiNet2.Domain.Search;

/// <summary>
/// Repository contract for the denormalised search index.
/// </summary>
public interface ISearchIndexRepository
{
    Task<SearchIndexEntry?> GetAsync(
        SearchObjectType objectType,
        string objectId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SearchIndexEntry entry, CancellationToken cancellationToken = default);

    void Update(SearchIndexEntry entry);

    void Remove(SearchIndexEntry entry);

    /// <summary>
    /// Full-text query with optional filters and cursor pagination.
    /// Results are restricted to entries the caller can see:
    /// <list type="bullet">
    ///   <item><description>public entries, or</description></item>
    ///   <item><description>entries owned by <paramref name="viewerId"/>, or</description></item>
    ///   <item><description>Study entries whose id is in <paramref name="viewerVisibleStudyIds"/>.</description></item>
    /// </list>
    /// Pass <c>null</c>/empty viewer args for anonymous callers (public only).
    /// </summary>
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        SearchObjectType? type,
        string? ownerId,
        string? tag,
        string? viewerId,
        IReadOnlyCollection<string>? viewerVisibleStudyIds,
        DateTime? cursorUpdatedBefore,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns recent public entries (no relevance ranking, just by date).
    /// </summary>
    Task<IReadOnlyList<SearchHit>> GetRecentPublicAsync(
        SearchObjectType? type,
        DateTime? cursorUpdatedBefore,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns top public entries by a simple recency-weighted score.
    /// </summary>
    Task<IReadOnlyList<SearchHit>> GetTrendingPublicAsync(
        SearchObjectType? type,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Composes the personal feed from authors, watched studies and self.
    /// </summary>
    Task<IReadOnlyList<SearchHit>> GetFeedAsync(
        IReadOnlyCollection<string> followedOwnerIds,
        IReadOnlyCollection<string> watchedStudyIds,
        string selfOwnerId,
        DateTime? cursorUpdatedBefore,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight projection returned by full-text queries.
/// </summary>
public sealed record SearchHit(
    Guid Id,
    SearchObjectType ObjectType,
    string ObjectId,
    string? OwnerId,
    string Title,
    string? Body,
    string? Tags,
    bool IsPublic,
    DateTime UpdatedAt,
    double Rank);
