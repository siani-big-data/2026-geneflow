using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Application.Discussions.Mappings;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Entities;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using DiscussionIdType = GeneFlow.ApiNet2.Domain.Discussions.DiscussionId;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.CreateComment;

public sealed class CreateCommentCommandHandler
    : ICommandHandler<CreateCommentCommand, Result<CommentDto>>
{
    private readonly IDiscussionRepository _discussionRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IDiscussionUnitOfWork _unitOfWork;
    private readonly IWatchRepository _watchRepository;
    private readonly INotificationUnitOfWork _notificationUnitOfWork;

    public CreateCommentCommandHandler(
        IDiscussionRepository discussionRepository,
        ICommentRepository commentRepository,
        IDiscussionUnitOfWork unitOfWork,
        IWatchRepository watchRepository,
        INotificationUnitOfWork notificationUnitOfWork)
    {
        _discussionRepository = discussionRepository;
        _commentRepository = commentRepository;
        _unitOfWork = unitOfWork;
        _watchRepository = watchRepository;
        _notificationUnitOfWork = notificationUnitOfWork;
    }

    public async Task<Result<CommentDto>> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        if (!DiscussionIdType.TryParse(request.DiscussionId, out var discussionId) || discussionId is null)
            return Result.Failure<CommentDto>(DiscussionErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<CommentDto>(CommentErrors.InsufficientPermissions);

        var discussion = await _discussionRepository.GetByIdAsync(discussionId, cancellationToken);
        if (discussion is null)
            return Result.Failure<CommentDto>(DiscussionErrors.NotFoundById(request.DiscussionId));

        if (discussion.IsLocked)
            return Result.Failure<CommentDto>(DiscussionErrors.Locked);

        var commentResult = Comment.Create(
            CommentParentType.Discussion,
            discussion.Id.ToString(),
            userId,
            request.BodyMarkdown);

        if (commentResult.IsFailure)
            return Result.Failure<CommentDto>(commentResult.Error);

        await _commentRepository.AddAsync(commentResult.Value, cancellationToken);

        // Auto-subscribe the comment author so they receive notifications about
        // future activity on this study's discussions (idempotent).
        await EnsureAuthorIsWatchingAsync(userId, discussion.StudyId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(commentResult.Value.ToDto(userId, isAdmin: false, reactions: Array.Empty<ReactionSummaryDto>()));
    }

    private async Task EnsureAuthorIsWatchingAsync(
        UserId userId,
        StudyId studyId,
        CancellationToken cancellationToken)
    {
        var existing = await _watchRepository.GetAsync(userId, studyId, cancellationToken);
        if (existing is null)
        {
            var watch = Watch.Create(userId, studyId, WatchLevel.All);
            await _watchRepository.AddAsync(watch, cancellationToken);
            await _notificationUnitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
