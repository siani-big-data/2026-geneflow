namespace GeneFlow.ApiNet2.Domain.Search;

/// <summary>
/// Unit of work for the Search bounded context.
/// </summary>
public interface ISearchUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
