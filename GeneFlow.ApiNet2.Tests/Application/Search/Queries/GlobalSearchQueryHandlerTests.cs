using GeneFlow.ApiNet2.Application.Search.Common;
using GeneFlow.ApiNet2.Application.Search.Queries.GlobalSearch;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;

namespace GeneFlow.ApiNet2.Tests.Application.Search.Queries;

public class GlobalSearchQueryHandlerTests
{
    private readonly ISearchIndexRepository _repo = Substitute.For<ISearchIndexRepository>();
    private readonly GlobalSearchQueryHandler _handler;

    public GlobalSearchQueryHandlerTests()
    {
        _handler = new GlobalSearchQueryHandler(_repo);
    }

    private static SearchHit Hit(SearchObjectType type, string id, string title,
        string? body = null, DateTime? updatedAt = null) =>
        new(Guid.NewGuid(), type, id, "U00000001", title, body, null, true,
            updatedAt ?? DateTime.UtcNow, Rank: 1.0);

    [Fact]
    public async Task Handle_EmptyQuery_ReturnsQueryRequiredError()
    {
        var result = await _handler.Handle(
            new GlobalSearchQuery("   ", null, null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.QueryRequired);
    }

    [Fact]
    public async Task Handle_InvalidType_ReturnsInvalidObjectTypeError()
    {
        var result = await _handler.Handle(
            new GlobalSearchQuery("foo", "NotAType", null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.InvalidObjectType);
    }

    [Fact]
    public async Task Handle_InvalidCursor_ReturnsInvalidCursorError()
    {
        var result = await _handler.Handle(
            new GlobalSearchQuery("foo", null, null, null, "not-a-cursor"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SearchErrors.InvalidCursor);
    }

    [Fact]
    public async Task Handle_ReturnsMatches_AndNoCursor_WhenPageNotFull()
    {
        _repo.SearchAsync(Arg.Any<string>(), Arg.Any<SearchObjectType?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<DateTime?>(), Arg.Any<Guid?>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<SearchHit> { Hit(SearchObjectType.Study, "S1", "T1") });

        var result = await _handler.Handle(
            new GlobalSearchQuery("foo", null, null, null, null, PageSize: 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OverfetchesByOne_AndEmitsCursor_WhenMore()
    {
        const int pageSize = 2;
        var hits = new List<SearchHit>
        {
            Hit(SearchObjectType.Study, "S1", "T1", updatedAt: DateTime.UtcNow.AddMinutes(-1)),
            Hit(SearchObjectType.Study, "S2", "T2", updatedAt: DateTime.UtcNow.AddMinutes(-2)),
            Hit(SearchObjectType.Study, "S3", "T3", updatedAt: DateTime.UtcNow.AddMinutes(-3)),
        };
        _repo.SearchAsync(Arg.Any<string>(), Arg.Any<SearchObjectType?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<DateTime?>(), Arg.Any<Guid?>(), pageSize + 1,
                Arg.Any<CancellationToken>())
            .Returns(hits);

        var result = await _handler.Handle(
            new GlobalSearchQuery("foo", null, null, null, null, PageSize: pageSize),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(pageSize);
        result.Value.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_BuildsSnippet_AroundMatchedToken()
    {
        var body = new string('a', 200) + " needle " + new string('b', 200);
        _repo.SearchAsync(Arg.Any<string>(), Arg.Any<SearchObjectType?>(),
                Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<DateTime?>(), Arg.Any<Guid?>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<SearchHit> { Hit(SearchObjectType.Discussion, "D1", "T", body) });

        var result = await _handler.Handle(
            new GlobalSearchQuery("needle", null, null, null, null), CancellationToken.None);

        result.Value.Items[0].Snippet.Should().Contain("needle");
        result.Value.Items[0].Snippet!.Length.Should().BeLessThan(body.Length);
    }
}
