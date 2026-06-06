using System.Collections.Concurrent;
using System.Threading.Channels;
using GeneFlow.ApiNet2.Application.Notifications.SSE;

namespace GeneFlow.ApiNet2.Infrastructure.Notifications.SSE;

/// <summary>
/// In-process pub/sub broker that fans out notification frames to one or
/// more SSE subscribers keyed by user id. A user can have multiple open
/// tabs / devices — each gets its own bounded channel.
/// </summary>
/// <remarks>
/// Subscribers receive a bounded <see cref="Channel{T}"/> (capacity
/// <see cref="ChannelCapacity"/>, drop-oldest). Slow consumers therefore
/// lose the oldest queued frames instead of blocking the publisher;
/// clients that miss frames can re-fetch the inbox via REST.
/// </remarks>
public sealed class NotificationEventBroker : INotificationEventBroker
{
    /// <summary>Per-subscriber channel buffer size. Drop-oldest on overflow.</summary>
    private const int ChannelCapacity = 32;

    private readonly ConcurrentDictionary<string, ConcurrentBag<Channel<NotificationEventDto>>> _subscribers =
        new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Channel<NotificationEventDto> Subscribe(string userId)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var channel = Channel.CreateBounded<NotificationEventDto>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        var bag = _subscribers.GetOrAdd(userId, _ => new ConcurrentBag<Channel<NotificationEventDto>>());
        bag.Add(channel);
        return channel;
    }

    /// <inheritdoc />
    public void Unsubscribe(string userId, Channel<NotificationEventDto> channel)
    {
        if (string.IsNullOrEmpty(userId) || channel is null)
            return;

        if (_subscribers.TryGetValue(userId, out var bag))
        {
            var remaining = bag.Where(c => !ReferenceEquals(c, channel)).ToArray();
            var rebuilt = new ConcurrentBag<Channel<NotificationEventDto>>(remaining);
            _subscribers[userId] = rebuilt;

            if (rebuilt.IsEmpty)
            {
                _subscribers.TryRemove(
                    new KeyValuePair<string, ConcurrentBag<Channel<NotificationEventDto>>>(userId, rebuilt));
            }
        }

        channel.Writer.TryComplete();
    }

    /// <inheritdoc />
    public Task PublishAsync(string userId, NotificationEventDto evt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || evt is null)
            return Task.CompletedTask;

        if (!_subscribers.TryGetValue(userId, out var bag) || bag.IsEmpty)
            return Task.CompletedTask;

        foreach (var channel in bag)
        {
            // TryWrite respects DropOldest and never blocks.
            channel.Writer.TryWrite(evt);
        }

        return Task.CompletedTask;
    }
}
