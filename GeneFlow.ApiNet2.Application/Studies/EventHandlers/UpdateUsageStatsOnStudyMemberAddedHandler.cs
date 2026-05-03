using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Studies.EventHandlers;

/// <summary>
/// Updates usage statistics when a member is added to a study.
/// Updates the max members count and the new member's studies total.
/// </summary>
public sealed class UpdateUsageStatsOnStudyMemberAddedHandler
    : IDomainEventHandler<StudyMemberAddedEvent>
{
    private readonly IUsageStatsRepository _usageRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly ILogger<UpdateUsageStatsOnStudyMemberAddedHandler> _logger;

    public UpdateUsageStatsOnStudyMemberAddedHandler(
        IUsageStatsRepository usageRepository,
        IStudyRepository studyRepository,
        ILogger<UpdateUsageStatsOnStudyMemberAddedHandler> logger)
    {
        _usageRepository = usageRepository;
        _studyRepository = studyRepository;
        _logger = logger;
    }

    public async Task Handle(StudyMemberAddedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating usage stats for member added. StudyId: {StudyId}, MemberId: {MemberId}, Role: {Role}",
            notification.StudyId,
            notification.MemberUserId,
            notification.Role.Name);

        try
        {
            // Update the new member's studies total
            var memberStats = await _usageRepository.GetByUserIdAsync(
                notification.MemberUserId,
                cancellationToken);

            if (memberStats is not null)
            {
                memberStats.IncrementStudiesTotal();
                await _usageRepository.SaveAsync(memberStats, cancellationToken);

                _logger.LogDebug(
                    "Member usage stats updated. User: {UserId}, StudiesTotal: {Count}",
                    notification.MemberUserId,
                    memberStats.StudiesTotal);
            }

            // Update the study owner's max members count
            var study = await _studyRepository.GetByIdWithMembersAsync(
                notification.StudyId,
                cancellationToken);

            if (study is not null)
            {
                var ownerStats = await _usageRepository.GetByUserIdAsync(
                    study.OwnerId,
                    cancellationToken);

                if (ownerStats is not null)
                {
                    var memberCount = study.Members.Count;
                    ownerStats.UpdateMaxMembersInStudy(memberCount);
                    await _usageRepository.SaveAsync(ownerStats, cancellationToken);

                    _logger.LogDebug(
                        "Owner usage stats updated. User: {UserId}, MaxMembersInStudy: {Count}",
                        study.OwnerId,
                        ownerStats.MaxMembersInStudy);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update usage stats for member added. StudyId: {StudyId}",
                notification.StudyId);
            // Don't rethrow - usage stats update shouldn't fail the member addition
        }
    }
}
