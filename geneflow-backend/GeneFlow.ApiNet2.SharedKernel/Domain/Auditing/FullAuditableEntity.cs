namespace GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;

/// <summary>
/// Base class for entities with full audit tracking including soft delete.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class FullAuditableEntity<TId> : AuditableEntity<TId>, ISoftDeletable
    where TId : notnull
{
    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; private set; }

    /// <inheritdoc />
    public string? DeletedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="FullAuditableEntity{TId}"/>.
    /// </summary>
    protected FullAuditableEntity() { }

    /// <summary>
    /// Initializes a new instance of <see cref="FullAuditableEntity{TId}"/> with the specified identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    protected FullAuditableEntity(TId id) : base(id) { }

    /// <summary>
    /// Soft deletes the entity.
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
    /// Restores a soft-deleted entity.
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
