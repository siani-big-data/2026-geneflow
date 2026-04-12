namespace GeneFlow.ApiNet2.Domain.Studies;

/// <summary>
/// Unit of Work interface for Study bounded context.
/// </summary>
public interface IStudyUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
