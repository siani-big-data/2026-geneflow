using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.AddReaction;

public sealed class AddReactionCommandHandler : ICommandHandler<AddReactionCommand, Result>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IReactionRepository _reactionRepository;
    private readonly IDiscussionUnitOfWork _unitOfWork;

    public AddReactionCommandHandler(
        ICommentRepository commentRepository,
        IReactionRepository reactionRepository,
        IDiscussionUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _reactionRepository = reactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddReactionCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(CommentErrors.InsufficientPermissions);

        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment is null)
            return Result.Failure(CommentErrors.NotFoundById(request.CommentId));

        var existing = await _reactionRepository.GetAsync(request.CommentId, userId, request.Emoji, cancellationToken);
        if (existing is not null)
            return Result.Success(); // idempotent

        var reactionResult = Reaction.Create(request.CommentId, userId, request.Emoji);
        if (reactionResult.IsFailure)
            return reactionResult;

        await _reactionRepository.AddAsync(reactionResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
