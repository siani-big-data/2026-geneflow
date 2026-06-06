using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Queries.GetUnreadCount;

public sealed record GetUnreadCountQuery(string UserId) : IQuery<Result<int>>, IRequireAuthentication;
