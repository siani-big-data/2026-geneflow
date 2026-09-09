using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Application.Discussions.Mappings;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using DiscussionIdType = GeneFlow.ApiNet2.Domain.Discussions.DiscussionId;

namespace GeneFlow.ApiNet2.Application.Discussions.Queries.GetDiscussion;

public sealed class GetDiscussionQueryHandler
    : IQueryHandler<GetDiscussionQuery, Result<DiscussionDto>>
{
    private readonly IDiscussionRepository _discussionRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IReactionRepository _reactionRepository;
    private readonly IStudyRepository _studyRepository;

    public GetDiscussionQueryHandler(
        IDiscussionRepository discussionRepository,
        ICommentRepository commentRepository,
        IReactionRepository reactionRepository,
        IStudyRepository studyRepository)
    {
        _discussionRepository = discussionRepository;
        _commentRepository = commentRepository;
        _reactionRepository = reactionRepository;
        _studyRepository = studyRepository;
    }

    public async Task<Result<DiscussionDto>> Handle(
        GetDiscussionQuery request,
        CancellationToken cancellationToken)
    {
        if (!DiscussionIdType.TryParse(request.DiscussionId, out var discussionId) || discussionId is null)
            return Result.Failure<DiscussionDto>(DiscussionErrors.NotFound);

        UserId? currentUser = null;
        if (request.UserId is not null && UserId.TryParse(request.UserId, out var parsed))
            currentUser = parsed;

        var discussion = await _discussionRepository.GetByIdAsync(discussionId, cancellationToken);
        if (discussion is null)
            return Result.Failure<DiscussionDto>(DiscussionErrors.NotFoundById(request.DiscussionId));

        // Authorization: members can always read; non-members only if study is public.
        var isAdmin = false;
        if (currentUser is not null)
        {
            var role = await _studyRepository.GetMemberRoleAsync(discussion.StudyId, currentUser, cancellationToken);
            if (role is null)
            {
                var isPublic = await _studyRepository.IsPublicStudyAsync(discussion.StudyId, cancellationToken);
                if (!isPublic)
                    return Result.Failure<DiscussionDto>(DiscussionErrors.InsufficientPermissions);
            }
            else
            {
                isAdmin = role == StudyRole.Owner || role == StudyRole.Admin;
            }
        }
        else
        {
            var isPublic = await _studyRepository.IsPublicStudyAsync(discussion.StudyId, cancellationToken);
            if (!isPublic)
                return Result.Failure<DiscussionDto>(DiscussionErrors.InsufficientPermissions);
        }

        var comments = await _commentRepository.GetForParentAsync(
            CommentParentType.Discussion, discussion.Id.ToString(), cancellationToken);

        var commentIds = comments.Select(c => c.Id).ToList();
        var reactions = commentIds.Count > 0
            ? await _reactionRepository.GetForCommentsAsync(commentIds, cancellationToken)
            : Array.Empty<Domain.Discussions.Entities.Reaction>();

        var reactionsByComment = DiscussionMappings.SummariseReactions(reactions, currentUser);

        var commentDtos = comments
            .Select(c => c.ToDto(
                currentUser,
                isAdmin,
                reactionsByComment.TryGetValue(c.Id, out var list) ? list : Array.Empty<ReactionSummaryDto>()))
            .ToList();

        return Result.Success(discussion.ToDto(commentDtos.Count, commentDtos));
    }
}
