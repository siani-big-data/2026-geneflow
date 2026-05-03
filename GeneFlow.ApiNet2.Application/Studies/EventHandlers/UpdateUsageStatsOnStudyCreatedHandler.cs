using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Studies.EventHandlers;

/// <summary>
/// Updates usage statistics when a study is created.
/// Increments the studies owned counter for the owner's billing period.
/// </summary>
public sealed class UpdateUsageStatsOnStudyCreatedHandler
    : IDomainEventHandler<StudyCreatedEvent>
{
    private readonly IUsageStatsRepository _usageRepository;
    private readonly ILogger<UpdateUsageStatsOnStudyCreatedHandler> _logger;

    public UpdateUsageStatsOnStudyCreatedHandler(
        IUsageStatsRepository usageRepository,
        ILogger<UpdateUsageStatsOnStudyCreatedHandler> logger)
    {
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task Handle(StudyCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating usage stats for study created. StudyId: {StudyId}, OwnerId: {OwnerId}, Title: {Title}",
            notification.StudyId,
            notification.OwnerId,
            notification.Title);

        try
        {
            var stats = await _usageRepository.GetByUserIdAsync(
                notification.OwnerId,
                cancellationToken);

            if (stats is not null)
            {
                stats.IncrementStudiesOwned();
                await _usageRepository.SaveAsync(stats, cancellationToken);

                _logger.LogDebug(
                    "Usage stats updated. User: {UserId}, StudiesOwned: {Count}",
                    notification.OwnerId,
                    stats.StudiesOwned);
            }
            else
            {
                _logger.LogWarning(
                    "Usage stats not found for user {UserId}. Stats will be created on next subscription update.",
                    notification.OwnerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update usage stats for study created. StudyId: {StudyId}",
                notification.StudyId);
            // Don't rethrow - usage stats update shouldn't fail the study creation
        }
    }
}
