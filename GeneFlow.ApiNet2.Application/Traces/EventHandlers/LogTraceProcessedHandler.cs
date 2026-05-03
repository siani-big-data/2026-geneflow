using GeneFlow.ApiNet2.Domain.Traces.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Traces.EventHandlers;

/// <summary>
/// Logs and handles trace processing completion.
/// This handler can be extended to update dashboards, send notifications, etc.
/// </summary>
public sealed class LogTraceProcessedHandler
    : IDomainEventHandler<TraceProcessedEvent>
{
    private readonly ILogger<LogTraceProcessedHandler> _logger;

    public LogTraceProcessedHandler(
        ILogger<LogTraceProcessedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TraceProcessedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trace processed successfully. TraceId: {TraceId}, StudyId: {StudyId}, AverageQuality: {Quality:F2}",
            notification.TraceId,
            notification.StudyId,
            notification.AverageQualityScore);

        return Task.CompletedTask;
    }
}
