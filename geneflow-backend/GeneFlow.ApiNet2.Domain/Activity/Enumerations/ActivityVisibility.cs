using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Activity.Enumerations;

/// <summary>
/// Determines who can see an activity event on the platform feeds.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><see cref="Private"/>: only the actor can see it (drafts, private edits).</item>
///   <item><see cref="StudyMembers"/>: visible to the members of the associated study.</item>
///   <item><see cref="Public"/>: visible on the public global feed.</item>
/// </list>
/// </remarks>
public sealed class ActivityVisibility : Enumeration<ActivityVisibility>
{
    public static readonly ActivityVisibility Private = new(1, nameof(Private));
    public static readonly ActivityVisibility StudyMembers = new(2, nameof(StudyMembers));
    public static readonly ActivityVisibility Public = new(3, nameof(Public));

    private ActivityVisibility(int id, string name) : base(id, name) { }
}
