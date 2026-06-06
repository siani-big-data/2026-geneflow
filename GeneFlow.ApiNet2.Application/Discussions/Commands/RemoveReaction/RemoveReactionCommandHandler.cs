using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.RemoveReaction;

public sealed class RemoveReactionCommandHandler : ICommandHandler<RemoveReactionCommand, Result>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IDiscussionUnitOfWork _unitOfWork;

    public RemoveReactionCommandHandler(
        IReactionRepository reactionRepository,
        IDiscussionUnitOfWork unitOfWork)
    {
        _reactionRepository = reactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
    {
        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(CommentErrors.InsufficientPermissions);

        var reaction = await _reactionRepository.GetAsync(request.CommentId, userId, request.Emoji, cancellationToken);
        if (reaction is null)
            return Result.Success(); // idempotent

        reaction.MarkRemoved();
        _reactionRepository.Remove(reaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
