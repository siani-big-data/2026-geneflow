using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications.Entities;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;

namespace GeneFlow.ApiNet2.Domain.Notifications;

public interface IWatchRepository
{
    Task<Watch?> GetAsync(UserId userId, StudyId studyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the user IDs of every watcher of the given study at the
    /// specified level. Used by the WatchNotifier fan-out pipeline.
    /// </summary>
    Task<IReadOnlyList<UserId>> GetWatchersAsync(
        StudyId studyId,
        WatchLevel level,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the study IDs that the given user is watching with the
    /// specified level. Used by the personal feed composer.
    /// </summary>
    Task<IReadOnlyList<StudyId>> GetWatchedStudyIdsAsync(
        UserId userId,
        WatchLevel level,
        CancellationToken cancellationToken = default);

    Task AddAsync(Watch watch, CancellationToken cancellationToken = default);

    void Update(Watch watch);

    void Remove(Watch watch);
}
