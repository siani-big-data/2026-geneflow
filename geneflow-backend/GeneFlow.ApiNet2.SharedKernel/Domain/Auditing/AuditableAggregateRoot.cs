using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;

/// <summary>
/// Base class for aggregate roots with audit tracking.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditable
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
    /// Initializes a new instance of <see cref="AuditableAggregateRoot{TId}"/>.
    /// </summary>
    protected AuditableAggregateRoot() { }

    /// <summary>
    /// Initializes a new instance of <see cref="AuditableAggregateRoot{TId}"/> with the specified identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    protected AuditableAggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Initializes creation audit with current UTC time.
    /// Call this in derived class constructors.
    /// </summary>
    protected void InitializeCreatedAt(string? createdBy = null)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Sets the creation audit information.
    /// </summary>
    public void SetCreationAudit(DateTime createdAt, string? createdBy = null)
    {
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Sets the modification audit information.
    /// </summary>
    public void SetModificationAudit(DateTime modifiedAt, string? modifiedBy = null)
    {
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Marks the entity as modified with current UTC time.
    /// </summary>
    protected void SetModified(string? modifiedBy = null)
    {
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }
}
