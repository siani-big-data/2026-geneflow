using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Commands.MarkAllRead;

public sealed class MarkAllReadCommandHandler : ICommandHandler<MarkAllReadCommand, Result<int>>
{
    private readonly INotificationRepository _repository;
    private readonly INotificationUnitOfWork _unitOfWork;

    public MarkAllReadCommandHandler(INotificationRepository repository, INotificationUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(MarkAllReadCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<int>(NotificationErrors.NotRecipient);

        var affected = await _repository.MarkAllReadAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(affected);
    }
}
