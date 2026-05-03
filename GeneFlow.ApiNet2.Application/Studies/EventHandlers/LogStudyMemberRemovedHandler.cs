using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Studies.EventHandlers;

/// <summary>
/// Updates usage statistics when a member is removed from a study.
/// Decrements the member's studies total.
/// </summary>
/// <remarks>
/// Exceptions are intentionally caught and logged without rethrowing because
/// usage-stats updates are a side effect that must not fail member removal.
/// </remarks>
public sealed class UpdateUsageStatsOnStudyMemberRemovedHandler
    : IDomainEventHandler<StudyMemberRemovedEvent>
{
    private readonly IUsageStatsRepository _usageRepository;
    private readonly ILogger<UpdateUsageStatsOnStudyMemberRemovedHandler> _logger;

    public UpdateUsageStatsOnStudyMemberRemovedHandler(
        IUsageStatsRepository usageRepository,
        ILogger<UpdateUsageStatsOnStudyMemberRemovedHandler> logger)
    {
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task Handle(StudyMemberRemovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating usage stats for member removed. StudyId: {StudyId}, MemberId: {MemberId}",
            notification.StudyId,
            notification.MemberUserId);

        try
        {
            var memberStats = await _usageRepository.GetByUserIdAsync(
                notification.MemberUserId,
                cancellationToken);

            if (memberStats is not null)
            {
                memberStats.DecrementStudiesTotal();
                await _usageRepository.SaveAsync(memberStats, cancellationToken);

                _logger.LogDebug(
                    "Member usage stats updated. User: {UserId}, StudiesTotal: {Count}",
                    notification.MemberUserId,
                    memberStats.StudiesTotal);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update usage stats for member removed. StudyId: {StudyId}",
                notification.StudyId);
        }
    }
}
