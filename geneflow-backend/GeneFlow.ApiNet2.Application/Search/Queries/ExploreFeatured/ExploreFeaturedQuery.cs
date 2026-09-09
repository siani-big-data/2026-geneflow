using GeneFlow.ApiNet2.Application.Search.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Search.Queries.ExploreFeatured;

public sealed record ExploreFeaturedQuery(int Limit = 20)
    : IQuery<Result<IReadOnlyList<ExploreItemDto>>>;
