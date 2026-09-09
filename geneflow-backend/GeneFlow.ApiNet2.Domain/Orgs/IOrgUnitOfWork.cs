namespace GeneFlow.ApiNet2.Domain.Orgs;

public interface IOrgUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
