using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.Application.Discussions.Mappings;
using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Queries.GetStudyDiscussions;

public sealed class GetStudyDiscussionsQueryHandler
    : IQueryHandler<GetStudyDiscussionsQuery, Result<PagedList<DiscussionDto>>>
{
    private readonly IDiscussionRepository _discussionRepository;
    private readonly ICommentRepository _commentRepository;

    public GetStudyDiscussionsQueryHandler(
        IDiscussionRepository discussionRepository,
        ICommentRepository commentRepository)
    {
        _discussionRepository = discussionRepository;
        _commentRepository = commentRepository;
    }

    public async Task<Result<PagedList<DiscussionDto>>> Handle(
        GetStudyDiscussionsQuery request,
        CancellationToken cancellationToken)
    {
        if (!StudyId.TryParse(request.StudyId, out var studyId) || studyId is null)
            return Result.Failure<PagedList<DiscussionDto>>(DiscussionErrors.NotFound);

        var page = await _discussionRepository.GetByStudyAsync(
            studyId,
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.Category,
            cancellationToken);

        var dtos = new List<DiscussionDto>(page.Items.Count);
        foreach (var d in page.Items)
        {
            var count = await _commentRepository.CountForParentAsync(
                CommentParentType.Discussion, d.Id.ToString(), cancellationToken);
            dtos.Add(d.ToDto(count));
        }

        return Result.Success(PagedList<DiscussionDto>.Create(dtos, page.PageNumber, page.PageSize, page.TotalCount));
    }
}
