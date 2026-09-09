using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Entities;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Repositories;

public sealed class WatchRepository : IWatchRepository
{
    private readonly NotificationContext _context;

    public WatchRepository(NotificationContext context)
    {
        _context = context;
    }

    public async Task<Watch?> GetAsync(
        UserId userId,
        StudyId studyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Watches
            .FirstOrDefaultAsync(w => w.UserId == userId && w.StudyId == studyId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserId>> GetWatchersAsync(
        StudyId studyId,
        WatchLevel level,
        CancellationToken cancellationToken = default)
    {
        return await _context.Watches
            .Where(w => w.StudyId == studyId && w.Level == level)
            .Select(w => w.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudyId>> GetWatchedStudyIdsAsync(
        UserId userId,
        WatchLevel level,
        CancellationToken cancellationToken = default)
    {
        return await _context.Watches
            .Where(w => w.UserId == userId && w.Level == level)
            .Select(w => w.StudyId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Watch watch, CancellationToken cancellationToken = default)
    {
        await _context.Watches.AddAsync(watch, cancellationToken);
    }

    public void Update(Watch watch)
    {
        _context.Watches.Update(watch);
    }

    public void Remove(Watch watch)
    {
        _context.Watches.Remove(watch);
    }
}
