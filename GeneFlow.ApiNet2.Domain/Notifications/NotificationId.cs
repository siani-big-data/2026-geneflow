using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Notifications;

/// <summary>
/// Strongly-typed identifier for a Notification aggregate.
/// Format: N########.
/// </summary>
public sealed class NotificationId : PrefixedId<NotificationId>
{
    public const string SequenceName = "notifications";

    protected override char Prefix => 'N';

    protected override int NumericLength => 8;

    public NotificationId(long value) : base(value) { }

    public static NotificationId Parse(string id) => Parse(id, v => new NotificationId(v));

    public static bool TryParse(string? id, out NotificationId? result)
        => TryParse(id, v => new NotificationId(v), out result);

    public static NotificationId FromSequence(long sequenceValue) => new(sequenceValue);
}
