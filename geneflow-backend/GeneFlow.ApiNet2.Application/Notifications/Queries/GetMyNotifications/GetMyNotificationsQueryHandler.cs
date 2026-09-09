using GeneFlow.ApiNet2.Application.Notifications.DTOs;
using GeneFlow.ApiNet2.Application.Notifications.Mappings;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler
    : IQueryHandler<GetMyNotificationsQuery, Result<PagedList<NotificationDto>>>
{
    private readonly INotificationRepository _repository;

    public GetMyNotificationsQueryHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedList<NotificationDto>>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<PagedList<NotificationDto>>(NotificationErrors.NotRecipient);

        var page = await _repository.GetForUserAsync(
            userId, request.PageNumber, request.PageSize, request.UnreadOnly, cancellationToken);

        return Result.Success(page.Map(n => n.ToDto()));
    }
}
