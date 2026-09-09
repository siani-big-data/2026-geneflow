using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Commands.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(
    string NotificationId,
    string UserId) : ICommand<Result>, IRequireAuthentication;
