using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Queries.GetUnreadCount;

public sealed class GetUnreadCountQueryHandler : IQueryHandler<GetUnreadCountQuery, Result<int>>
{
    private readonly INotificationRepository _repository;

    public GetUnreadCountQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<int>(NotificationErrors.NotRecipient);

        var count = await _repository.CountUnreadAsync(userId, cancellationToken);
        return Result.Success(count);
    }
}
