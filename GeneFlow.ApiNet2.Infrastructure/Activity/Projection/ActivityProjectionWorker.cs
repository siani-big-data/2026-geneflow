using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Infrastructure.Activity.Projection;

/// <summary>
/// Background service that consumes every event category the platform publishes and
/// projects each message into an <see cref="ActivityEvent"/> through
/// <see cref="IActivityEventProjector"/>.
/// </summary>
/// <remarks>
/// <para>
/// One Redis Streams consumer group is opened per category in parallel; failed handlers
/// rethrow so the bus does not acknowledge the message and replays on the next poll.
/// Idempotency is enforced by <see cref="IActivityEventRepository.ExistsBySourceMessageIdAsync"/>
/// combined with the unique index on <c>source_message_id</c>.
/// </para>
/// </remarks>
public sealed class ActivityProjectionWorker : BackgroundService
{
    /// <summary>Consumer group name used across every category for the activity projector.</summary>
    public const string ConsumerGroup = "activity-projector";

    /// <summary>
    /// Bus categories the projector consumes. Mirrors <c>EventCategoryResolver</c> so any
    /// event published by the platform reaches the activity feed.
    /// </summary>
    public static readonly IReadOnlyList<string> Categories =
    [
        "users",
        "profiles",
        "studies",
        "traces",
        "alignments",
        "analysis",
        "pipelines",
        "subscriptions",
        "plans",
        "usage",
        "orgs"
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventBusSubscriber _subscriber;
    private readonly IActivityEventProjector _projector;
    private readonly ILogger<ActivityProjectionWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityProjectionWorker"/>.
    /// </summary>
    public ActivityProjectionWorker(
        IServiceScopeFactory scopeFactory,
        IEventBusSubscriber subscriber,
        IActivityEventProjector projector,
        ILogger<ActivityProjectionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _subscriber = subscriber;
        _projector = projector;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Activity projection worker starting...");

        var tasks = Categories
            .Select(category => ProcessCategoryAsync(category, stoppingToken))
            .ToArray();

        await Task.WhenAll(tasks);

        _logger.LogInformation("Activity projection worker stopped");
    }

    private async Task ProcessCategoryAsync(string category, CancellationToken cancellationToken)
    {
        try
        {
            await _subscriber.SubscribeAsync(
                category,
                ConsumerGroup,
                message => HandleAsync(message, category, cancellationToken),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error in activity projector for category {Category}", category);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error in activity projector for category {Category}", category);
        }
    }

    private async Task HandleAsync(EventMessage message, string category, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IActivityEventRepository>();

            // Idempotency check: same source message replayed by Redis after a crash.
            if (!string.IsNullOrWhiteSpace(message.MessageId)
                && await repository.ExistsBySourceMessageIdAsync(message.MessageId, cancellationToken))
            {
                _logger.LogDebug(
                    "Skipping already projected message {MessageId} ({EventType})",
                    message.MessageId, message.EventType);
                return;
            }

            var activityEvent = _projector.Project(message, category);
            if (activityEvent is null)
            {
                _logger.LogInformation(
                    "Activity projector skipped event {EventType} on {Category} (no verb/object mapping)",
                    message.EventType, category);
                return;
            }

            await repository.AddAsync(activityEvent, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Projected activity event {ActivityEventId} from {EventType} on {Category} (actor={Actor}, study={Study}, visibility={Visibility})",
                activityEvent.Id, message.EventType, category,
                activityEvent.ActorUserId ?? "<null>",
                activityEvent.StudyId ?? "<null>",
                activityEvent.Visibility.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to project activity event from {Category}/{EventType} (message {MessageId})",
                category, message.EventType, message.MessageId);
            throw;
        }
    }
}
