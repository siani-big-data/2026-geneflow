using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Notifications.Enumerations;

/// <summary>
/// Categorises a notification so the UI can pick an icon and the user
/// can filter their inbox.
/// </summary>
public sealed class NotificationType : Enumeration<NotificationType>
{
    /// <summary>A new top-level discussion was created on a watched study.</summary>
    public static readonly NotificationType DiscussionCreated = new(1, nameof(DiscussionCreated));

    /// <summary>A new comment was posted on a watched discussion.</summary>
    public static readonly NotificationType CommentCreated = new(2, nameof(CommentCreated));

    /// <summary>Reserved for future @mention support.</summary>
    public static readonly NotificationType Mentioned = new(3, nameof(Mentioned));

    /// <summary>Catch-all for legacy / system events.</summary>
    public static readonly NotificationType System = new(99, nameof(System));

    private NotificationType(int id, string name) : base(id, name) { }
}
