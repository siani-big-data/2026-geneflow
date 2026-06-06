using System.Text;

namespace GeneFlow.ApiNet2.Application.Search.Common;

/// <summary>
/// Opaque cursor used by search / explore / feed endpoints.
/// Encodes (UpdatedAt ticks, entry Guid) for keyset pagination.
/// </summary>
public static class SearchCursor
{
    public static string Encode(DateTime updatedAt, Guid id)
    {
        var raw = $"{updatedAt.Ticks}:{id:N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryDecode(string? cursor, out DateTime updatedAt, out Guid id)
    {
        updatedAt = default;
        id = default;
        if (string.IsNullOrWhiteSpace(cursor)) return false;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(':');
            if (parts.Length != 2) return false;
            if (!long.TryParse(parts[0], out var ticks)) return false;
            if (!Guid.TryParseExact(parts[1], "N", out var parsedId)) return false;

            updatedAt = new DateTime(ticks, DateTimeKind.Utc);
            id = parsedId;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
