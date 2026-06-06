using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Commands.MarkAllRead;

public sealed record MarkAllReadCommand(string UserId) : ICommand<Result<int>>, IRequireAuthentication;
