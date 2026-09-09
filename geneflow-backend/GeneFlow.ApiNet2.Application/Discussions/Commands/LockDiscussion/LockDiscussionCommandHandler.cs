using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using DiscussionIdType = GeneFlow.ApiNet2.Domain.Discussions.DiscussionId;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.LockDiscussion;

public sealed class LockDiscussionCommandHandler
    : ICommandHandler<LockDiscussionCommand, Result>
{
    private readonly IDiscussionRepository _discussionRepository;
    private readonly IDiscussionUnitOfWork _unitOfWork;

    public LockDiscussionCommandHandler(
        IDiscussionRepository discussionRepository,
        IDiscussionUnitOfWork unitOfWork)
    {
        _discussionRepository = discussionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(LockDiscussionCommand request, CancellationToken cancellationToken)
    {
        if (!DiscussionIdType.TryParse(request.DiscussionId, out var discussionId) || discussionId is null)
            return Result.Failure(DiscussionErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure(DiscussionErrors.InsufficientPermissions);

        var discussion = await _discussionRepository.GetByIdAsync(discussionId, cancellationToken);
        if (discussion is null)
            return Result.Failure(DiscussionErrors.NotFoundById(request.DiscussionId));

        var result = request.Lock ? discussion.Lock(userId) : discussion.Unlock(userId);
        if (result.IsFailure)
            return result;

        _discussionRepository.Update(discussion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
