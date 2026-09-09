using GeneFlow.ApiNet2.Domain.Traces.Events;
using GeneFlow.ApiNet2.Domain.Usage;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Traces.EventHandlers;

/// <summary>
/// Updates usage statistics when a trace is uploaded.
/// Increments the trace upload count for the current billing period.
/// </summary>
/// <remarks>
/// Exceptions are intentionally caught and logged without rethrowing because
/// usage-stats updates are a side effect that must not fail the upload.
/// </remarks>
public sealed class UpdateUsageStatsOnTraceUploadedHandler
    : IDomainEventHandler<TraceUploadedEvent>
{
    private readonly IUsageStatsRepository _usageRepository;
    private readonly ILogger<UpdateUsageStatsOnTraceUploadedHandler> _logger;

    public UpdateUsageStatsOnTraceUploadedHandler(
        IUsageStatsRepository usageRepository,
        ILogger<UpdateUsageStatsOnTraceUploadedHandler> logger)
    {
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task Handle(TraceUploadedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating usage stats for trace upload. TraceId: {TraceId}, StudyId: {StudyId}, UploadedBy: {UserId}",
            notification.TraceId,
            notification.StudyId,
            notification.UploadedBy);

        try
        {
            var periodKey = BillingPeriodKey.Current();
            var stats = await _usageRepository.GetByUserIdAsync(
                notification.UploadedBy,
                cancellationToken);

            if (stats is not null)
            {
                stats.IncrementTraces();
                await _usageRepository.SaveAsync(stats, cancellationToken);

                _logger.LogDebug(
                    "Usage stats updated. User: {UserId}, TracesThisPeriod: {Count}",
                    notification.UploadedBy,
                    stats.TracesThisPeriod);
            }
            else
            {
                _logger.LogWarning(
                    "Usage stats not found for user {UserId}. Stats will be created on next subscription update.",
                    notification.UploadedBy);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update usage stats for trace upload. TraceId: {TraceId}",
                notification.TraceId);
        }
    }
}
