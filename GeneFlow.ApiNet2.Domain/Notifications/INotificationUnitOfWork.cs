namespace GeneFlow.ApiNet2.Domain.Notifications;

public interface INotificationUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
