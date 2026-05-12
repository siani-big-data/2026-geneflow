using System.Collections.Concurrent;
using System.Threading.Channels;
using GeneFlow.ApiNet2.Application.Pipelines.Events;

namespace GeneFlow.ApiNet2.Infrastructure.Pipelines.Sse;

/// <summary>
/// In-memory, in-process pub/sub broker that fans out pipeline-execution
/// progress events (started / step.completed / completed / failed) to one
/// or more SSE subscribers, keyed by execution id.
/// </summary>
/// <remarks>
/// Subscribers receive a bounded <see cref="Channel{T}"/> (capacity
/// <see cref="ChannelCapacity"/>, drop-oldest). Slow consumers therefore lose
/// the oldest queued events instead of blocking the publisher; clients that
/// reconnect simply re-fetch the execution state via REST.
/// </remarks>
public sealed class PipelineExecutionEventBroker
{
    /// <summary>Per-subscriber channel buffer size. Drop-oldest on overflow.</summary>
    private const int ChannelCapacity = 32;

    private readonly ConcurrentDictionary<string, ConcurrentBag<Channel<PipelineExecutionEventDto>>> _subscribers =
        new(StringComparer.Ordinal);

    public Channel<PipelineExecutionEventDto> Subscribe(string executionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(executionId);

        var channel = Channel.CreateBounded<PipelineExecutionEventDto>(
            new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        var bag = _subscribers.GetOrAdd(executionId, _ => new ConcurrentBag<Channel<PipelineExecutionEventDto>>());
        bag.Add(channel);
        return channel;
    }

    public void Unsubscribe(string executionId, Channel<PipelineExecutionEventDto> channel)
    {
        if (string.IsNullOrEmpty(executionId) || channel is null)
            return;

        if (_subscribers.TryGetValue(executionId, out var bag))
        {
            var remaining = bag.Where(c => !ReferenceEquals(c, channel)).ToArray();
            var rebuilt = new ConcurrentBag<Channel<PipelineExecutionEventDto>>(remaining);
            _subscribers[executionId] = rebuilt;

            if (rebuilt.IsEmpty)
            {
                _subscribers.TryRemove(new KeyValuePair<string, ConcurrentBag<Channel<PipelineExecutionEventDto>>>(executionId, rebuilt));
            }
        }

        channel.Writer.TryComplete();
    }

    public Task PublishAsync(string executionId, PipelineExecutionEventDto evt)
    {
        if (string.IsNullOrEmpty(executionId) || evt is null)
            return Task.CompletedTask;

        if (!_subscribers.TryGetValue(executionId, out var bag) || bag.IsEmpty)
            return Task.CompletedTask;

        foreach (var channel in bag)
        {
            channel.Writer.TryWrite(evt);
        }

        return Task.CompletedTask;
    }
}
