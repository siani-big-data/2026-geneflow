using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.ExploreFeatured;

/// <summary>
/// Featured surfaces public studies tagged as featured. v1 reuses the
/// trending pipeline restricted to studies — the manual curation flag
/// can land in a follow-up commit once a "featured" boolean exists on
/// <c>SearchIndexEntry</c>.
/// </summary>
public sealed class ExploreFeaturedQueryHandler
    : IQueryHandler<ExploreFeaturedQuery, Result<IReadOnlyList<ExploreItemDto>>>
{
    private const int MaxLimit = 50;
    private readonly ISearchIndexRepository _repository;

    public ExploreFeaturedQueryHandler(ISearchIndexRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ExploreItemDto>>> Handle(
        ExploreFeaturedQuery request, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, MaxLimit);
        var hits = await _repository.GetTrendingPublicAsync(
            SearchObjectType.Study, limit, cancellationToken);

        IReadOnlyList<ExploreItemDto> items = hits
            .Select(h => new ExploreItemDto(
                h.ObjectType.Name, h.ObjectId, h.OwnerId, h.Title, h.Body, h.Tags, h.UpdatedAt, 0))
            .ToList();

        return Result.Success(items);
    }
}
