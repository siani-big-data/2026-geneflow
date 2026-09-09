using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;

namespace GeneFlow.ApiNet2.Infrastructure.Activity.Projection;

/// <summary>
/// Translates a raw <see cref="EventMessage"/> read from the event bus into a projected
/// <see cref="ActivityEvent"/>, returning <c>null</c> when the event should not appear on
/// any feed (e.g. unmapped event types).
/// </summary>
public interface IActivityEventProjector
{
    /// <summary>
    /// Builds an <see cref="ActivityEvent"/> from the given bus envelope.
    /// </summary>
    /// <param name="message">Raw envelope read from a stream consumer group.</param>
    /// <param name="category">Stream category the message was read from (e.g. <c>studies</c>).</param>
    /// <returns>The activity event to persist, or <c>null</c> to skip.</returns>
    ActivityEvent? Project(EventMessage message, string category);
}
