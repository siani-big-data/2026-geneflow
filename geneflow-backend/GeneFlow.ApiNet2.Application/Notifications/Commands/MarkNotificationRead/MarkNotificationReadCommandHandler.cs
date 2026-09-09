using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler : ICommandHandler<MarkNotificationReadCommand, Result>
{
    private readonly INotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;

    public MarkNotificationReadCommandHandler(
        INotificationRepository repository,
        INotificationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        if (!NotificationId.TryParse(request.NotificationId, out var notificationId) || notificationId is null)
            return Result.Failure(NotificationErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(NotificationErrors.NotRecipient);

        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null)
            return Result.Failure(NotificationErrors.NotFoundById(request.NotificationId));

        var result = notification.MarkRead(userId);
        if (result.IsFailure)
            return result;

        _repository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
