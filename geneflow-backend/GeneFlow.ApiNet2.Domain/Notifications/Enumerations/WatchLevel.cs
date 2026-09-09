using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Notifications.Enumerations;

/// <summary>
/// The strength of a user's subscription to a watchable subject (currently
/// only Study). Phase 4 ships <c>All</c> and <c>None</c>; <c>Mentions</c>
/// is reserved until @mentions land.
/// </summary>
public sealed class WatchLevel : Enumeration<WatchLevel>
{
    /// <summary>Receive notifications for every event on the subject.</summary>
    public static readonly WatchLevel All = new(1, nameof(All));

    /// <summary>Receive no notifications (explicit opt-out).</summary>
    public static readonly WatchLevel None = new(2, nameof(None));

    /// <summary>Reserved for the @mention phase.</summary>
    public static readonly WatchLevel Mentions = new(3, nameof(Mentions));

    private WatchLevel(int id, string name) : base(id, name) { }
}
