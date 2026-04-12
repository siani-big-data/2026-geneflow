using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Studies.Enumerations;

/// <summary>
/// Study lifecycle status.
/// </summary>
public sealed class StudyStatus : Enumeration<StudyStatus>
{
    public static readonly StudyStatus Draft = new(1, nameof(Draft), "Draft");
    public static readonly StudyStatus Active = new(2, nameof(Active), "Active");
    public static readonly StudyStatus Completed = new(3, nameof(Completed), "Completed");
    public static readonly StudyStatus Published = new(4, nameof(Published), "Published");
    public static readonly StudyStatus Archived = new(5, nameof(Archived), "Archived");

    public string DisplayName { get; }

    private StudyStatus(int id, string name, string displayName) : base(id, name)
    {
        DisplayName = displayName;
    }

    /// <summary>
    /// Checks if transition to the given status is valid.
    /// </summary>
    public bool CanTransitionTo(StudyStatus newStatus)
    {
        return (this, newStatus) switch
        {
            _ when this == Draft && newStatus == Active => true,
            _ when this == Active && newStatus == Completed => true,
            _ when this == Active && newStatus == Draft => true,
            _ when this == Completed && newStatus == Published => true,
            _ when this == Completed && newStatus == Active => true,
            _ when this == Published && newStatus == Archived => true,
            _ when this == Archived && newStatus == Published => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if the study is publicly visible.
    /// </summary>
    public bool IsPubliclyVisible => this == Published;
}
