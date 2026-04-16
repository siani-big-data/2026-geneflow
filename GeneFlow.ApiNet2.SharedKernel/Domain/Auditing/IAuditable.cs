namespace GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;

/// <summary>
/// Interface for entities that track creation timestamp.
/// </summary>
public interface ICreationAuditable
{
    /// <summary>
    /// When the entity was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Who created the entity (user ID, username, etc.).
    /// </summary>
    string? CreatedBy { get; }
}

/// <summary>
/// Interface for entities that track modification timestamp.
/// </summary>
public interface IModificationAuditable
{
    /// <summary>
    /// When the entity was last modified.
    /// </summary>
    DateTime? ModifiedAt { get; }

    /// <summary>
    /// Who last modified the entity.
    /// </summary>
    string? ModifiedBy { get; }
}

/// <summary>
/// Interface for entities that track both creation and modification.
/// </summary>
public interface IAuditable : ICreationAuditable, IModificationAuditable;

/// <summary>
/// Interface for entities that support soft deletion with audit.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// When the entity was deleted.
    /// </summary>
    DateTime? DeletedAt { get; }

    /// <summary>
    /// Who deleted the entity.
    /// </summary>
    string? DeletedBy { get; }
}

/// <summary>
/// Full audit interface including soft delete.
/// </summary>
public interface IFullAuditable : IAuditable, ISoftDeletable;
