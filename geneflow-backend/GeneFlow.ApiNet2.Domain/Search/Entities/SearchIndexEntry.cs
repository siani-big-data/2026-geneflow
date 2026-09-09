using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Search.Entities;

/// <summary>
/// Denormalised projection of a domain object into a Postgres full-text
/// searchable row. One entry per (ObjectType, ObjectId). The <c>Tsv</c>
/// column is generated server-side by Postgres from Title / Body / Tags.
/// </summary>
public sealed class SearchIndexEntry : Entity<Guid>
{
    public const int MaxTitleLength = 300;

    public SearchObjectType ObjectType { get; private set; } = null!;

    /// <summary>
    /// String form of the indexed object's identifier (prefixed ID for
    /// studies/discussions, GUID for traces, etc.). Composite uniqueness
    /// with <see cref="ObjectType"/>.
    /// </summary>
    public string ObjectId { get; private set; } = null!;

    /// <summary>
    /// Owner of the object — used to filter results by author / visibility.
    /// May be null for system-owned entries.
    /// </summary>
    public string? OwnerId { get; private set; }

    /// <summary>Main searchable label (weighted 'A' in the tsvector).</summary>
    public string Title { get; private set; } = null!;

    /// <summary>Free-form body / description (weighted 'B').</summary>
    public string? Body { get; private set; }

    /// <summary>Comma-separated tags (weighted 'C').</summary>
    public string? Tags { get; private set; }

    /// <summary>
    /// Whether the object is reachable by unauthenticated users.
    /// Lets the search query skip a JOIN to the source aggregate when
    /// filtering by visibility.
    /// </summary>
    public bool IsPublic { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SearchIndexEntry() { }

    private SearchIndexEntry(
        Guid id,
        SearchObjectType objectType,
        string objectId,
        string? ownerId,
        string title,
        string? body,
        string? tags,
        bool isPublic)
    {
        Id = id;
        ObjectType = objectType;
        ObjectId = objectId;
        OwnerId = ownerId;
        Title = title;
        Body = body;
        Tags = tags;
        IsPublic = isPublic;

        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Result<SearchIndexEntry> Create(
        SearchObjectType objectType,
        string objectId,
        string? ownerId,
        string title,
        string? body,
        string? tags,
        bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            return Result.Failure<SearchIndexEntry>(SearchErrors.ObjectIdRequired);

        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<SearchIndexEntry>(SearchErrors.TitleRequired);

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > MaxTitleLength)
            trimmedTitle = trimmedTitle[..MaxTitleLength];

        var entry = new SearchIndexEntry(
            Guid.NewGuid(),
            objectType,
            objectId.Trim(),
            ownerId,
            trimmedTitle,
            body,
            tags,
            isPublic);

        return entry;
    }

    /// <summary>
    /// Replaces the searchable fields with fresh values from the source.
    /// </summary>
    public void Update(string title, string? body, string? tags, bool isPublic, string? ownerId)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;

        Title = title.Trim().Length > MaxTitleLength
            ? title.Trim()[..MaxTitleLength]
            : title.Trim();
        Body = body;
        Tags = tags;
        IsPublic = isPublic;
        OwnerId = ownerId;
        UpdatedAt = DateTime.UtcNow;
    }
}
