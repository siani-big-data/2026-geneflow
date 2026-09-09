using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.ExploreTrending;

public sealed class ExploreTrendingQueryHandler
    : IQueryHandler<ExploreTrendingQuery, Result<IReadOnlyList<ExploreItemDto>>>
{
    private const int MaxLimit = 50;
    private readonly ISearchIndexRepository _repository;

    public ExploreTrendingQueryHandler(ISearchIndexRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ExploreItemDto>>> Handle(
        ExploreTrendingQuery request, CancellationToken cancellationToken)
    {
        SearchObjectType? type = null;
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (!SearchObjectType.TryFromName(request.Type, out var parsed))
                return Result.Failure<IReadOnlyList<ExploreItemDto>>(SearchErrors.InvalidObjectType);
            type = parsed;
        }

        var limit = Math.Clamp(request.Limit, 1, MaxLimit);
        var hits = await _repository.GetTrendingPublicAsync(type, limit, cancellationToken);

        IReadOnlyList<ExploreItemDto> items = hits
            .Select(h => new ExploreItemDto(
                h.ObjectType.Name, h.ObjectId, h.OwnerId, h.Title, h.Body, h.Tags, h.UpdatedAt, 0))
            .ToList();

        return Result.Success(items);
    }
}
