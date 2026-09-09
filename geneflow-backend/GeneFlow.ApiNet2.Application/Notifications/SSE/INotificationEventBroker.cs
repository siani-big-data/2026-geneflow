using System.Threading.Channels;

namespace GeneFlow.ApiNet2.Application.Notifications.SSE;

/// <summary>
/// Per-user in-memory publish/subscribe channel for notification SSE
/// frames. The interface lives in the Application layer so domain event
/// handlers can publish without depending on Infrastructure; concrete
/// channel-based implementation is in Infrastructure.
/// </summary>
public interface INotificationEventBroker
{
    Channel<NotificationEventDto> Subscribe(string userId);

    void Unsubscribe(string userId, Channel<NotificationEventDto> channel);

    Task PublishAsync(string userId, NotificationEventDto evt, CancellationToken cancellationToken = default);
}
