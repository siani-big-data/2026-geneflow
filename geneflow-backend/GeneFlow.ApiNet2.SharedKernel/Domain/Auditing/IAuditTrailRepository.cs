namespace GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;

/// <summary>
/// Repository for storing and querying audit trail entries.
/// Implemented in Infrastructure.
/// </summary>
public interface IAuditTrailRepository
{
    /// <summary>
    /// Saves an audit entry.
    /// </summary>
    Task SaveAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple audit entries.
    /// </summary>
    Task SaveManyAsync(IEnumerable<AuditEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries for a specific entity.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries by user.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByUserAsync(
        string userId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit entries within a time range.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
