namespace GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;

/// <summary>
/// Represents an audit trail entry for tracking changes.
/// </summary>
public sealed class AuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// The entity type that was changed.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// The primary key of the entity.
    /// </summary>
    public string EntityId { get; }

    /// <summary>
    /// The type of change (Created, Modified, Deleted).
    /// </summary>
    public AuditAction Action { get; }

    /// <summary>
    /// When the change occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Who made the change.
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// The old values (for updates and deletes).
    /// </summary>
    public IReadOnlyDictionary<string, object?> OldValues { get; }

    /// <summary>
    /// The new values (for creates and updates).
    /// </summary>
    public IReadOnlyDictionary<string, object?> NewValues { get; }

    /// <summary>
    /// The properties that were changed.
    /// </summary>
    public IReadOnlyList<string> ChangedProperties { get; }

    /// <summary>
    /// Initializes a new audit entry.
    /// </summary>
    /// <param name="entityType">The type of entity that was changed.</param>
    /// <param name="entityId">The entity's primary key.</param>
    /// <param name="action">The type of change.</param>
    /// <param name="timestamp">When the change occurred.</param>
    /// <param name="userId">Who made the change.</param>
    /// <param name="oldValues">Previous property values.</param>
    /// <param name="newValues">New property values.</param>
    /// <param name="changedProperties">List of changed property names.</param>
    public AuditEntry(
        string entityType,
        string entityId,
        AuditAction action,
        DateTime timestamp,
        string? userId,
        IReadOnlyDictionary<string, object?> oldValues,
        IReadOnlyDictionary<string, object?> newValues,
        IReadOnlyList<string> changedProperties)
    {
        Id = Guid.NewGuid();
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        Timestamp = timestamp;
        UserId = userId;
        OldValues = oldValues;
        NewValues = newValues;
        ChangedProperties = changedProperties;
    }
}
