namespace GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;

/// <summary>
/// Base class for aggregate roots with full audit tracking including soft delete.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public abstract class FullAuditableAggregateRoot<TId> : AuditableAggregateRoot<TId>, ISoftDeletable
    where TId : notnull
{
    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; private set; }

    /// <inheritdoc />
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="FullAuditableAggregateRoot{TId}"/>.
    /// </summary>
    protected FullAuditableAggregateRoot() { }

    /// <summary>
    /// Initializes a new instance of <see cref="FullAuditableAggregateRoot{TId}"/> with the specified identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    protected FullAuditableAggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Soft deletes the aggregate.
    /// </summary>
    public virtual void SoftDelete(DateTime deletedAt, string? deletedBy = null)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Soft deletes the aggregate with current UTC time.
    /// </summary>
    public virtual void SoftDelete(string? deletedBy = null)
    {
        SoftDelete(DateTime.UtcNow, deletedBy);
    }

    /// <summary>
    /// Restores a soft-deleted aggregate.
    /// </summary>
    public virtual void Restore()
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
