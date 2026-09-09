namespace GeneFlow.ApiNet2.SharedKernel.Domain;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft-deleted entities are marked as deleted but not physically removed from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether the entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// When the entity was soft-deleted.
    /// </summary>
    DateTime? DeletedAt { get; }
}
