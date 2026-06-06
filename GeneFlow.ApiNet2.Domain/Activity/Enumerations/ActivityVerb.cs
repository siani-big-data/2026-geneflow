using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Activity.Enumerations;

/// <summary>
/// Action performed by an actor over an object in the activity stream.
/// Modelled after the GitHub events vocabulary (Created/Updated/Deleted/...).
/// </summary>
public sealed class ActivityVerb : Enumeration<ActivityVerb>
{
    public static readonly ActivityVerb Created = new(1, nameof(Created));
    public static readonly ActivityVerb Updated = new(2, nameof(Updated));
    public static readonly ActivityVerb Deleted = new(3, nameof(Deleted));
    public static readonly ActivityVerb Uploaded = new(4, nameof(Uploaded));
    public static readonly ActivityVerb Started = new(5, nameof(Started));
    public static readonly ActivityVerb Completed = new(6, nameof(Completed));
    public static readonly ActivityVerb Failed = new(7, nameof(Failed));
    public static readonly ActivityVerb Cancelled = new(8, nameof(Cancelled));
    public static readonly ActivityVerb Joined = new(9, nameof(Joined));
    public static readonly ActivityVerb Left = new(10, nameof(Left));
    public static readonly ActivityVerb Invited = new(11, nameof(Invited));
    public static readonly ActivityVerb Published = new(12, nameof(Published));
    public static readonly ActivityVerb Archived = new(13, nameof(Archived));
    public static readonly ActivityVerb Registered = new(14, nameof(Registered));
    public static readonly ActivityVerb Applied = new(15, nameof(Applied));
    public static readonly ActivityVerb Undone = new(16, nameof(Undone));
    public static readonly ActivityVerb Requested = new(17, nameof(Requested));
    public static readonly ActivityVerb Unknown = new(99, nameof(Unknown));

    private ActivityVerb(int id, string name) : base(id, name) { }
}
