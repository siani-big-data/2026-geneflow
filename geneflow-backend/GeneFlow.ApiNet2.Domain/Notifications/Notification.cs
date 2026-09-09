using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Notifications.Events;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Notifications;

/// <summary>
/// A per-user inbox entry. One <see cref="Notification"/> row exists per
/// (recipient, source event) pair — fan-out happens upstream in the
/// WatchNotifier domain event handler.
/// </summary>
public sealed class Notification : FullAuditableAggregateRoot<NotificationId>
{
    public const int MaxSubjectLength = 300;

    public UserId RecipientId { get; private set; } = null!;
    public NotificationType Type { get; private set; } = null!;

    /// <summary>
    /// Human-readable subject line shown in the tray (e.g. "Alice commented on 'New idea'").
    /// </summary>
    public string Subject { get; private set; } = null!;

    /// <summary>
    /// Deep-link the UI navigates to when the user clicks the notification.
    /// </summary>
    public string? Url { get; private set; }

    /// <summary>
    /// Source aggregate / event reference for client-side de-duplication
    /// across SSE re-deliveries. Free-form (e.g. "comment:&lt;guid&gt;").
    /// </summary>
    public string? SourceRef { get; private set; }

    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Notification() : base() { }

    private Notification(
        NotificationId id,
        UserId recipientId,
        NotificationType type,
        string subject,
        string? url,
        string? sourceRef) : base(id)
    {
        RecipientId = recipientId;
        Type = type;
        Subject = subject;
        Url = url;
        SourceRef = sourceRef;
        IsRead = false;

        InitializeCreatedAt(recipientId.ToString());
    }

    public static Result<Notification> Create(
        NotificationId id,
        UserId recipientId,
        NotificationType type,
        string subject,
        string? url,
        string? sourceRef = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Result.Failure<Notification>(NotificationErrors.SubjectRequired);

        if (subject.Length > MaxSubjectLength)
            return Result.Failure<Notification>(NotificationErrors.SubjectTooLong);

        var notification = new Notification(id, recipientId, type, subject.Trim(), url, sourceRef);

        notification.RaiseDomainEvent(new NotificationCreatedEvent(
            notification.Id,
            notification.RecipientId,
            notification.Type,
            notification.Subject,
            notification.Url));

        return notification;
    }

    /// <summary>
    /// Marks the notification as read. Idempotent — calling on an
    /// already-read notification is a no-op and returns Success.
    /// </summary>
    public Result MarkRead(UserId actor)
    {
        if (actor != RecipientId)
            return Result.Failure(NotificationErrors.NotRecipient);

        if (IsRead)
            return Result.Success();

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        SetModified(actor.ToString());

        RaiseDomainEvent(new NotificationReadEvent(Id, RecipientId, ReadAt.Value));
        return Result.Success();
    }
}
