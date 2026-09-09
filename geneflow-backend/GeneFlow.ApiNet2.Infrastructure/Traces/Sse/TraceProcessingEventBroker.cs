using System.Collections.Concurrent;
using System.Threading.Channels;
using GeneFlow.ApiNet2.Application.Traces.Events;

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Sse;

/// <summary>
/// In-memory, in-process pub/sub broker that fans out trace-processing
/// transition events (started/completed/failed) to one or more SSE
/// subscribers, keyed by trace id.
/// </summary>
/// <remarks>
/// Subscribers receive a bounded <see cref="Channel{T}"/> (capacity
/// <see cref="ChannelCapacity"/>, drop-oldest). Slow consumers therefore lose
/// the oldest queued events instead of blocking the publisher; clients that
/// reconnect simply re-fetch the trace via REST.
/// </remarks>
public sealed class TraceProcessingEventBroker
{
    /// <summary>Per-subscriber channel buffer size. Drop-oldest on overflow.</summary>
    private const int ChannelCapacity = 32;

    private readonly ConcurrentDictionary<string, ConcurrentBag<Channel<TraceProcessingEventDto>>> _subscribers =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a new subscriber for <paramref name="traceId"/> and returns
    /// the reader-side channel it should pump events from.
    /// </summary>
    public Channel<TraceProcessingEventDto> Subscribe(string traceId)
    {
        ArgumentException.ThrowIfNullOrEmpty(traceId);

        var channel = Channel.CreateBounded<TraceProcessingEventDto>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        var bag = _subscribers.GetOrAdd(traceId, _ => new ConcurrentBag<Channel<TraceProcessingEventDto>>());
        bag.Add(channel);
        return channel;
    }

    /// <summary>
    /// Unsubscribes <paramref name="channel"/>. Rebuilds the bag without the
    /// entry (ConcurrentBag has no remove-by-item) and completes the channel.
    /// </summary>
    public void Unsubscribe(string traceId, Channel<TraceProcessingEventDto> channel)
    {
        if (string.IsNullOrEmpty(traceId) || channel is null)
            return;

        if (_subscribers.TryGetValue(traceId, out var bag))
        {
            var remaining = bag.Where(c => !ReferenceEquals(c, channel)).ToArray();
            var rebuilt = new ConcurrentBag<Channel<TraceProcessingEventDto>>(remaining);
            _subscribers[traceId] = rebuilt;

            if (rebuilt.IsEmpty)
            {
                _subscribers.TryRemove(new KeyValuePair<string, ConcurrentBag<Channel<TraceProcessingEventDto>>>(traceId, rebuilt));
            }
        }

        channel.Writer.TryComplete();
    }

    /// <summary>
    /// Publishes <paramref name="evt"/> to every active subscriber of
    /// <paramref name="traceId"/>. Non-blocking; drop-oldest enforced by the channel.
    /// </summary>
    public Task PublishAsync(string traceId, TraceProcessingEventDto evt)
    {
        if (string.IsNullOrEmpty(traceId) || evt is null)
            return Task.CompletedTask;

        if (!_subscribers.TryGetValue(traceId, out var bag) || bag.IsEmpty)
            return Task.CompletedTask;

        foreach (var channel in bag)
        {
            channel.Writer.TryWrite(evt);
        }

        return Task.CompletedTask;
    }
}
