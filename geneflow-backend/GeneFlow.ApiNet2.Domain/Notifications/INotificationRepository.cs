using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default);

    Task<PagedList<Notification>> GetForUserAsync(
        UserId userId,
        int pageNumber,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(UserId userId, CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);

    void Update(Notification notification);

    Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default);
}
