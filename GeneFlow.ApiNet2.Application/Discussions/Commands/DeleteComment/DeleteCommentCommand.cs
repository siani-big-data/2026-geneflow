using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.DeleteComment;

public sealed record DeleteCommentCommand(
    Guid CommentId,
    string UserId,
    bool IsAdmin) : ICommand<Result>, IRequireAuthentication;
