using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationContext _context;
    private readonly ISequenceGenerator _sequenceGenerator;

    public NotificationRepository(NotificationContext context, ISequenceGenerator sequenceGenerator)
    {
        _context = context;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<PagedList<Notification>> GetForUserAsync(
        UserId userId,
        int pageNumber,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => n.RecipientId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<Notification>.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<int> CountUnreadAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<int> MarkAllReadAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Bulk update — avoids loading every row into the change tracker.
        return await _context.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                u => u
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now)
                    .SetProperty(n => n.ModifiedAt, now),
                cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddRangeAsync(notifications, cancellationToken);
    }

    public void Update(Notification notification)
    {
        _context.Notifications.Update(notification);
    }

    public Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default)
    {
        return _sequenceGenerator.NextAsync(NotificationId.SequenceName, cancellationToken);
    }
}
