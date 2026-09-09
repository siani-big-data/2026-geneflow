using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;

/// <summary>
/// Base class for entities with audit tracking.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable
    where TId : notnull
{
    /// <inheritdoc />
    public DateTime CreatedAt { get; private set; }

    /// <inheritdoc />
    public string? CreatedBy { get; private set; }

    /// <inheritdoc />
    public DateTime? ModifiedAt { get; private set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="AuditableEntity{TId}"/>.
    /// </summary>
    protected AuditableEntity() { }

    /// <summary>
    /// Initializes a new instance of <see cref="AuditableEntity{TId}"/> with the specified identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    protected AuditableEntity(TId id) : base(id) { }

    /// <summary>
    /// Sets the creation audit information.
    /// Called by the persistence layer when saving a new entity.
    /// </summary>
    public void SetCreationAudit(DateTime createdAt, string? createdBy = null)
    {
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Sets the modification audit information.
    /// Called by the persistence layer when updating an entity.
    /// </summary>
    public void SetModificationAudit(DateTime modifiedAt, string? modifiedBy = null)
    {
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }
}
