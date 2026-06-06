using GeneFlow.ApiNet2.Application.Search.Common;

namespace GeneFlow.ApiNet2.Tests.Application.Search.Common;

public class SearchCursorTests
{
    [Fact]
    public void Encode_ThenDecode_RoundTrips()
    {
        var ts = new DateTime(2026, 6, 6, 17, 30, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();

        var cursor = SearchCursor.Encode(ts, id);

        SearchCursor.TryDecode(cursor, out var ts2, out var id2).Should().BeTrue();
        ts2.Should().Be(ts);
        id2.Should().Be(id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryDecode_ReturnsFalse_ForEmptyInput(string? input)
    {
        SearchCursor.TryDecode(input, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryDecode_ReturnsFalse_ForGarbageBase64()
    {
        SearchCursor.TryDecode("!!!not-base64!!!", out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryDecode_ReturnsFalse_ForWellFormedButWrongShape()
    {
        var bad = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("12345"));
        SearchCursor.TryDecode(bad, out _, out _).Should().BeFalse();
    }
}
