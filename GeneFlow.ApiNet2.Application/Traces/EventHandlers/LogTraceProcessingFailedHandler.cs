using GeneFlow.ApiNet2.Domain.Traces.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Traces.EventHandlers;

/// <summary>
/// Logs trace processing failures for monitoring and alerting.
/// This handler ensures that all processing failures are properly logged
/// for operational visibility and debugging purposes.
/// </summary>
public sealed class LogTraceProcessingFailedHandler
    : IDomainEventHandler<TraceProcessingFailedEvent>
{
    private readonly ILogger<LogTraceProcessingFailedHandler> _logger;

    public LogTraceProcessingFailedHandler(
        ILogger<LogTraceProcessingFailedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TraceProcessingFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Trace processing failed. TraceId: {TraceId}, StudyId: {StudyId}, Reason: {Reason}",
            notification.TraceId,
            notification.StudyId,
            notification.Reason);

        // In a production system, this could also:
        // - Send alerts to monitoring systems
        // - Notify study administrators
        // - Update dashboards

        return Task.CompletedTask;
    }
}
