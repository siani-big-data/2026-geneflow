using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

/// <summary>
/// Pipeline lifecycle status.
/// </summary>
public sealed class PipelineStatus : Enumeration<PipelineStatus>
{
    public static readonly PipelineStatus Draft = new(1, nameof(Draft), "Draft");
    public static readonly PipelineStatus Active = new(2, nameof(Active), "Active");
    public static readonly PipelineStatus Archived = new(3, nameof(Archived), "Archived");

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    private PipelineStatus(int id, string name, string displayName)
        : base(id, name)
    {
        DisplayName = displayName;
    }

    /// <summary>
    /// Checks if transition to the given status is valid.
    /// </summary>
    public bool CanTransitionTo(PipelineStatus newStatus)
    {
        return (this, newStatus) switch
        {
            // From Draft
            _ when this == Draft && newStatus == Active => true,
            _ when this == Draft && newStatus == Archived => true,

            // From Active
            _ when this == Active && newStatus == Draft => true,
            _ when this == Active && newStatus == Archived => true,

            // From Archived - can restore to draft
            _ when this == Archived && newStatus == Draft => true,

            _ => false
        };
    }

    /// <summary>
    /// Whether the pipeline can be executed.
    /// </summary>
    public bool CanExecute => this == Active;

    /// <summary>
    /// Whether the pipeline can be edited.
    /// </summary>
    public bool CanEdit => this == Draft;

    /// <summary>
    /// Whether the pipeline can be deleted.
    /// </summary>
    public bool CanDelete => this != Archived;
}
