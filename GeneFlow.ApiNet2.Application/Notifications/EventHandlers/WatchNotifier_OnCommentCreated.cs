using GeneFlow.ApiNet2.Application.Notifications.SSE;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;
using DiscussionIdType = GeneFlow.ApiNet2.Domain.Discussions.DiscussionId;

namespace GeneFlow.ApiNet2.Application.Notifications.EventHandlers;

/// <summary>
/// Translates a domain CommentCreated event into one Notification per
/// watcher of the parent study (excluding the comment author), and
/// pushes a live SSE frame to each watcher's channel.
/// </summary>
public sealed class WatchNotifier_OnCommentCreated : IDomainEventHandler<CommentCreatedEvent>
{
    private readonly IDiscussionRepository _discussionRepository;
    private readonly IWatchRepository _watchRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly INotificationEventBroker _broker;
    private readonly ILogger<WatchNotifier_OnCommentCreated> _logger;

    public WatchNotifier_OnCommentCreated(
        IDiscussionRepository discussionRepository,
        IWatchRepository watchRepository,
        INotificationRepository notificationRepository,
        INotificationUnitOfWork unitOfWork,
        INotificationEventBroker broker,
        ILogger<WatchNotifier_OnCommentCreated> logger)
    {
        _discussionRepository = discussionRepository;
        _watchRepository = watchRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _broker = broker;
        _logger = logger;
    }

    public async Task Handle(CommentCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Only fan-out for Discussion comments in Phase 4.
            if (notification.ParentType != CommentParentType.Discussion)
                return;

            if (!DiscussionIdType.TryParse(notification.ParentId, out var discussionId) || discussionId is null)
                return;

            var discussion = await _discussionRepository.GetByIdAsync(discussionId, cancellationToken);
            if (discussion is null) return;

            var watchers = await _watchRepository.GetWatchersAsync(
                discussion.StudyId, WatchLevel.All, cancellationToken);

            var recipients = watchers
                .Where(w => w != notification.AuthorId)
                .Distinct()
                .ToList();

            if (recipients.Count == 0) return;

            var bodyPreview = notification.BodyMarkdown.Length > 140
                ? notification.BodyMarkdown[..140] + "…"
                : notification.BodyMarkdown;

            var subject = $"New comment on \"{discussion.Title}\"";
            var url = $"/studies/{discussion.StudyId}/discussions/{discussion.Id}";

            var created = new List<Notification>(recipients.Count);
            foreach (var recipient in recipients)
            {
                var seq = await _notificationRepository.GetNextSequenceValueAsync(cancellationToken);
                var id = NotificationId.FromSequence(seq);
                var notif = Notification.Create(
                    id,
                    recipient,
                    NotificationType.CommentCreated,
                    subject,
                    url,
                    sourceRef: $"comment:{notification.CommentId}");

                if (notif.IsSuccess)
                    created.Add(notif.Value);
            }

            if (created.Count > 0)
            {
                await _notificationRepository.AddRangeAsync(created, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var n in created)
                {
                    var evt = new NotificationEventDto(
                        n.Id.ToString(),
                        n.RecipientId.ToString(),
                        n.Type.Name,
                        n.Subject,
                        n.Url,
                        n.CreatedAt);

                    await _broker.PublishAsync(n.RecipientId.ToString(), evt, cancellationToken);
                }
            }

            _ = bodyPreview; // reserved: future notification body
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Watch notifier failed for CommentCreated event {CommentId}",
                notification.CommentId);
            // Notification fan-out failure must never fail the original command.
        }
    }
}
