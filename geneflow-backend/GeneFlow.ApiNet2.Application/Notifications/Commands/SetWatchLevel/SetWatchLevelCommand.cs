using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Notifications.Commands.SetWatchLevel;

public sealed record SetWatchLevelCommand(
    string UserId,
    string StudyId,
    string Level) : ICommand<Result>, IRequireAuthentication;
