using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Application.Discussions.Mappings;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Queries.GetCommentsForParent;

public sealed class GetCommentsForParentQueryHandler
    : IQueryHandler<GetCommentsForParentQuery, Result<IReadOnlyList<CommentDto>>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IReactionRepository _reactionRepository;

    public GetCommentsForParentQueryHandler(
        ICommentRepository commentRepository,
        IReactionRepository reactionRepository)
    {
        _commentRepository = commentRepository;
        _reactionRepository = reactionRepository;
    }

    public async Task<Result<IReadOnlyList<CommentDto>>> Handle(
        GetCommentsForParentQuery request,
        CancellationToken cancellationToken)
    {
        if (!CommentParentType.TryFromName(request.ParentType, out var parentType) || parentType is null)
            return Result.Failure<IReadOnlyList<CommentDto>>(CommentErrors.NotFound);

        UserId? currentUser = null;
        if (request.UserId is not null && UserId.TryParse(request.UserId, out var u))
            currentUser = u;

        var comments = await _commentRepository.GetForParentAsync(parentType, request.ParentId, cancellationToken);
        var commentIds = comments.Select(c => c.Id).ToList();
        var reactions = commentIds.Count > 0
            ? await _reactionRepository.GetForCommentsAsync(commentIds, cancellationToken)
            : Array.Empty<Domain.Discussions.Entities.Reaction>();

        var summary = DiscussionMappings.SummariseReactions(reactions, currentUser);

        var dtos = comments
            .Select(c => c.ToDto(
                currentUser,
                isAdmin: false,
                summary.TryGetValue(c.Id, out var list) ? list : Array.Empty<ReactionSummaryDto>()))
            .ToList();

        return Result.Success<IReadOnlyList<CommentDto>>(dtos);
    }
}
