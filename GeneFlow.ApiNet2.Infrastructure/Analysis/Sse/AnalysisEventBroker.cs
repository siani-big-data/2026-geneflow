using System.Collections.Concurrent;
using System.Threading.Channels;
using GeneFlow.ApiNet2.Application.Analysis.Events;

namespace GeneFlow.ApiNet2.Infrastructure.Analysis.Sse;

/// <summary>
/// In-memory, in-process pub/sub broker that fans out analysis-completion
/// events to one or more SSE subscribers, keyed by trace id.
/// </summary>
/// <remarks>
/// Subscribers receive a bounded <see cref="Channel{T}"/> (capacity
/// <see cref="ChannelCapacity"/>, drop-oldest). Slow consumers therefore lose
/// the oldest queued events instead of blocking the publisher; clients that
/// reconnect simply re-fetch the relevant analysis result via REST.
/// </remarks>
public sealed class AnalysisEventBroker
{
    /// <summary>Per-subscriber channel buffer size. Drop-oldest on overflow.</summary>
    private const int ChannelCapacity = 32;

    private readonly ConcurrentDictionary<string, ConcurrentBag<Channel<AnalysisEventDto>>> _subscribers =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a new subscriber for <paramref name="traceId"/> and returns
    /// the reader-side channel it should pump events from.
    /// </summary>
    public Channel<AnalysisEventDto> Subscribe(string traceId)
    {
        ArgumentException.ThrowIfNullOrEmpty(traceId);

        var channel = Channel.CreateBounded<AnalysisEventDto>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        var bag = _subscribers.GetOrAdd(traceId, _ => new ConcurrentBag<Channel<AnalysisEventDto>>());
        bag.Add(channel);
        return channel;
    }

    /// <summary>
    /// Unsubscribes <paramref name="channel"/>. <see cref="ConcurrentBag{T}"/>
    /// has no remove-by-item, so we rebuild the bag without the entry.
    /// Completes the channel so any pending reader exits cleanly.
    /// </summary>
    public void Unsubscribe(string traceId, Channel<AnalysisEventDto> channel)
    {
        if (string.IsNullOrEmpty(traceId) || channel is null)
            return;

        if (_subscribers.TryGetValue(traceId, out var bag))
        {
            var remaining = bag.Where(c => !ReferenceEquals(c, channel)).ToArray();
            var rebuilt = new ConcurrentBag<Channel<AnalysisEventDto>>(remaining);
            _subscribers[traceId] = rebuilt;

            if (rebuilt.IsEmpty)
            {
                // Best-effort cleanup; if a new subscriber slid in concurrently
                // it will simply re-create the bag via GetOrAdd next time.
                _subscribers.TryRemove(new KeyValuePair<string, ConcurrentBag<Channel<AnalysisEventDto>>>(traceId, rebuilt));
            }
        }

        channel.Writer.TryComplete();
    }

    /// <summary>
    /// Publishes <paramref name="evt"/> to every active subscriber of
    /// <paramref name="traceId"/>. Non-blocking: writes that fail (closed
    /// channel) are swallowed. Drop-oldest is enforced by the channel itself.
    /// </summary>
    public Task PublishAsync(string traceId, AnalysisEventDto evt)
    {
        if (string.IsNullOrEmpty(traceId) || evt is null)
            return Task.CompletedTask;

        if (!_subscribers.TryGetValue(traceId, out var bag) || bag.IsEmpty)
            return Task.CompletedTask;

        foreach (var channel in bag)
        {
            // TryWrite respects DropOldest and never blocks.
            channel.Writer.TryWrite(evt);
        }

        return Task.CompletedTask;
    }
}
