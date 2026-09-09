namespace GeneFlow.ApiNet2.SharedKernel.Domain;

/// <summary>
/// Interface for entities that track creation and modification timestamps.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// When the entity was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// When the entity was last modified.
    /// </summary>
    DateTime? ModifiedAt { get; }
}
