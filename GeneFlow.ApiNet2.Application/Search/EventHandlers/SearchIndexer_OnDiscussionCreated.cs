using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Events;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Entities;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Search.EventHandlers;

/// <summary>
/// Projects a newly-created discussion into the search index.
/// Visibility derives from the parent study.
/// </summary>
public sealed class SearchIndexer_OnDiscussionCreated : IDomainEventHandler<DiscussionCreatedEvent>
{
    private readonly ISearchIndexRepository _index;
    private readonly ISearchUnitOfWork _unitOfWork;
    private readonly IDiscussionRepository _discussionRepository;
    private readonly IStudyRepository _studyRepository;
    private readonly ILogger<SearchIndexer_OnDiscussionCreated> _logger;

    public SearchIndexer_OnDiscussionCreated(
        ISearchIndexRepository index,
        ISearchUnitOfWork unitOfWork,
        IDiscussionRepository discussionRepository,
        IStudyRepository studyRepository,
        ILogger<SearchIndexer_OnDiscussionCreated> logger)
    {
        _index = index;
        _unitOfWork = unitOfWork;
        _discussionRepository = discussionRepository;
        _studyRepository = studyRepository;
        _logger = logger;
    }

    public async Task Handle(DiscussionCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var discussion = await _discussionRepository.GetByIdAsync(
                notification.DiscussionId, cancellationToken);
            if (discussion is null) return;

            // visibility tracks parent study
            var isPublic = await _studyRepository.IsPublicStudyAsync(
                discussion.StudyId, cancellationToken);

            var objectId = discussion.Id.ToString();
            var ownerId = discussion.AuthorId.ToString();
            var tags = string.IsNullOrWhiteSpace(discussion.Category)
                ? null : discussion.Category;

            var existing = await _index.GetAsync(SearchObjectType.Discussion, objectId, cancellationToken);
            if (existing is not null)
            {
                existing.Update(discussion.Title, null, tags, isPublic, ownerId);
                _index.Update(existing);
            }
            else
            {
                var entry = SearchIndexEntry.Create(
                    SearchObjectType.Discussion, objectId, ownerId,
                    discussion.Title, null, tags, isPublic);
                if (entry.IsFailure) return;
                await _index.AddAsync(entry.Value, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SearchIndexer failed for DiscussionCreated {DiscussionId}",
                notification.DiscussionId);
        }
    }
}
