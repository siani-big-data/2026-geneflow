namespace GeneFlow.ApiNet2.Domain.Discussions;

public interface IDiscussionUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
