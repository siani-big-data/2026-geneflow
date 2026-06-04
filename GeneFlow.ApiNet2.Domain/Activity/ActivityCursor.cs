using System.Globalization;
using System.Text;

namespace GeneFlow.ApiNet2.Domain.Activity;

/// <summary>
/// Opaque cursor used for keyset pagination of activity feeds.
/// Encodes the tuple <c>(OccurredAt, Id)</c> so that pages remain stable even when new events
/// are projected concurrently with reads.
/// </summary>
/// <remarks>
/// The wire format is base64url(occurredAtTicksUtc + ":" + guid), where ticks are written in
/// invariant culture. The cursor is opaque to clients; callers MUST treat it as a black box.
/// </remarks>
public sealed class ActivityCursor : IEquatable<ActivityCursor>
{
    /// <summary>UTC timestamp of the boundary event.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>Identifier of the boundary event.</summary>
    public Guid Id { get; }

    private ActivityCursor(DateTimeOffset occurredAt, Guid id)
    {
        OccurredAt = occurredAt;
        Id = id;
    }

    /// <summary>
    /// Builds a cursor from the boundary event of the previous page.
    /// </summary>
    public static ActivityCursor From(DateTimeOffset occurredAt, Guid id)
        => new(occurredAt.ToUniversalTime(), id);

    /// <summary>
    /// Encodes the cursor as an opaque base64url string.
    /// </summary>
    public string Encode()
    {
        var raw = string.Create(
            CultureInfo.InvariantCulture,
            $"{OccurredAt.UtcTicks}:{Id:N}");
        var bytes = Encoding.UTF8.GetBytes(raw);
        return ToBase64Url(bytes);
    }

    /// <summary>
    /// Decodes an opaque cursor produced by <see cref="Encode"/>.
    /// Returns <c>null</c> when the input is null or empty or malformed.
    /// </summary>
    public static ActivityCursor? Decode(string? encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return null;
        }

        try
        {
            var bytes = FromBase64Url(encoded);
            var raw = Encoding.UTF8.GetString(bytes);
            var separator = raw.IndexOf(':');
            if (separator <= 0 || separator >= raw.Length - 1)
            {
                return null;
            }

            var ticks = long.Parse(raw.AsSpan(0, separator), CultureInfo.InvariantCulture);
            var id = Guid.ParseExact(raw.AsSpan(separator + 1), "N");
            var occurredAt = new DateTimeOffset(ticks, TimeSpan.Zero);
            return new ActivityCursor(occurredAt, id);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string ToBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] FromBase64Url(string encoded)
    {
        var s = encoded.Replace('-', '+').Replace('_', '/');
        var padding = (4 - (s.Length % 4)) % 4;
        if (padding > 0)
        {
            s = s.PadRight(s.Length + padding, '=');
        }
        return Convert.FromBase64String(s);
    }

    public bool Equals(ActivityCursor? other)
        => other is not null && OccurredAt == other.OccurredAt && Id == other.Id;

    public override bool Equals(object? obj) => obj is ActivityCursor other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(OccurredAt, Id);

    public override string ToString() => Encode();
}
