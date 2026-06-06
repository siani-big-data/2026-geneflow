using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.DeleteComment;

public sealed class DeleteCommentCommandHandler : ICommandHandler<DeleteCommentCommand, Result>
{
    private readonly ICommentRepository _commentRepository;
    private readonly IDiscussionUnitOfWork _unitOfWork;

    public DeleteCommentCommandHandler(
        ICommentRepository commentRepository,
        IDiscussionUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(CommentErrors.InsufficientPermissions);

        var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment is null)
            return Result.Failure(CommentErrors.NotFoundById(request.CommentId));

        var deleteResult = comment.Delete(userId, request.IsAdmin);
        if (deleteResult.IsFailure)
            return deleteResult;

        _commentRepository.Update(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
