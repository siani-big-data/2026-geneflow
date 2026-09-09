using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Notifications.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(
    string UserId,
    bool UnreadOnly = false,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<Result<PagedList<NotificationDto>>>, IRequireAuthentication;
