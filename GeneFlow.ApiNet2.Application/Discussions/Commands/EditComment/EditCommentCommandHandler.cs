using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Application.Discussions.Mappings;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.EditComment;

public sealed class EditCommentCommandHandler
    : ICommandHandler<EditCommentCommand, Result<CommentDto>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IReactionRepository _reactionRepository;
    private readonly IDiscussionUnitOfWork _unitOfWork;

    public EditCommentCommandHandler(
        ICommentRepository commentRepository,
        IReactionRepository reactionRepository,
        IDiscussionUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _reactionRepository = reactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CommentDto>> Handle(EditCommentCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<CommentDto>(CommentErrors.InsufficientPermissions);

        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment is null)
            return Result.Failure<CommentDto>(CommentErrors.NotFoundById(request.CommentId));

        var editResult = comment.Edit(userId, request.BodyMarkdown);
        if (editResult.IsFailure)
            return Result.Failure<CommentDto>(editResult.Error);

        _commentRepository.Update(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var reactions = await _reactionRepository.GetForCommentsAsync(new[] { comment.Id }, cancellationToken);
        var summary = DiscussionMappings.SummariseReactions(reactions, userId);
        var reactionList = summary.TryGetValue(comment.Id, out var list) ? list : Array.Empty<ReactionSummaryDto>();

        return Result.Success(comment.ToDto(userId, isAdmin: false, reactionList));
    }
}
