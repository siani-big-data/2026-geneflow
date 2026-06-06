using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.AddReaction;

public sealed record AddReactionCommand(
    Guid CommentId,
    string UserId,
    string Emoji) : ICommand<Result>, IRequireAuthentication;
