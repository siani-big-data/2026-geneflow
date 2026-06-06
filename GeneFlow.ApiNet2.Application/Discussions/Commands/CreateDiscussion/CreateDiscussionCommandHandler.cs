using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Application.Discussions.Mappings;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Commands.CreateDiscussion;

public sealed class CreateDiscussionCommandHandler
    : ICommandHandler<CreateDiscussionCommand, Result<DiscussionDto>>
{
    private readonly IDiscussionRepository _discussionRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IDiscussionUnitOfWork _unitOfWork;

    public CreateDiscussionCommandHandler(
        IDiscussionRepository discussionRepository,
        ICommentRepository commentRepository,
        IDiscussionUnitOfWork unitOfWork)
    {
        _discussionRepository = discussionRepository;
        _commentRepository = commentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DiscussionDto>> Handle(
        CreateDiscussionCommand request,
        CancellationToken cancellationToken)
    {
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<DiscussionDto>(DiscussionErrors.NotFound);

        if (!UserId.TryParse(request.UserId, out var userId) || userId is null)
            return Result.Failure<DiscussionDto>(DiscussionErrors.InsufficientPermissions);

        var sequence = await _discussionRepository.GetNextSequenceValueAsync(cancellationToken);
        var discussionId = DiscussionId.FromSequence(sequence);

        var discussionResult = Discussion.Create(discussionId, studyId, userId, request.Title, request.Category);
        if (discussionResult.IsFailure)
            return Result.Failure<DiscussionDto>(discussionResult.Error);

        var discussion = discussionResult.Value;
        await _discussionRepository.AddAsync(discussion, cancellationToken);

        // Seed the first comment so the thread isn't empty on creation.
        var commentResult = Comment.Create(
            CommentParentType.Discussion,
            discussion.Id.ToString(),
            userId,
            request.FirstCommentBody);

        if (commentResult.IsFailure)
            return Result.Failure<DiscussionDto>(commentResult.Error);

        await _commentRepository.AddAsync(commentResult.Value, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(discussion.ToDto(commentCount: 1));
    }
}
