using GeneFlow.ApiNet2.Application.Notifications.SSE;
using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Notifications.EventHandlers;

public sealed class WatchNotifier_OnDiscussionCreated : IDomainEventHandler<DiscussionCreatedEvent>
{
    private readonly IWatchRepository _watchRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly INotificationEventBroker _broker;
    private readonly ILogger<WatchNotifier_OnDiscussionCreated> _logger;

    public WatchNotifier_OnDiscussionCreated(
        IWatchRepository watchRepository,
        INotificationRepository notificationRepository,
        INotificationUnitOfWork unitOfWork,
        INotificationEventBroker broker,
        ILogger<WatchNotifier_OnDiscussionCreated> logger)
    {
        _watchRepository = watchRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _broker = broker;
        _logger = logger;
    }

    public async Task Handle(DiscussionCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var watchers = await _watchRepository.GetWatchersAsync(
                notification.StudyId, WatchLevel.All, cancellationToken);

            var recipients = watchers
                .Where(w => w != notification.AuthorId)
                .Distinct()
                .ToList();

            if (recipients.Count == 0)
                return;

            var subject = $"New discussion: \"{notification.Title}\"";
            var url = $"/studies/{notification.StudyId}/discussions/{notification.DiscussionId}";

            var created = new List<Notification>(recipients.Count);
            foreach (var recipient in recipients)
            {
                var seq = await _notificationRepository.GetNextSequenceValueAsync(cancellationToken);
                var id = NotificationId.FromSequence(seq);
                var result = Notification.Create(
                    id,
                    recipient,
                    NotificationType.DiscussionCreated,
                    subject,
                    url,
                    sourceRef: $"discussion:{notification.DiscussionId}");

                if (result.IsSuccess)
                    created.Add(result.Value);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Watch notifier failed for DiscussionCreated event {DiscussionId}",
                notification.DiscussionId);
        }
    }
}
