using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Infrastructure.Search.Persistence.Context;

namespace GeneFlow.ApiNet2.Infrastructure.Search.Persistence.Repositories;

public sealed class SearchUnitOfWork : ISearchUnitOfWork
{
    private readonly SearchContext _context;

    public SearchUnitOfWork(SearchContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
