using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Entities;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Events;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Search.EventHandlers;

/// <summary>
/// Projects a newly-created Study into the search index. Idempotent.
/// Fetches the full study to capture description + tags + visibility.
/// </summary>
public sealed class SearchIndexer_OnStudyCreated : IDomainEventHandler<StudyCreatedEvent>
{
    private readonly ISearchIndexRepository _index;
    private readonly ISearchUnitOfWork _unitOfWork;
    private readonly IStudyRepository _studyRepository;
    private readonly ILogger<SearchIndexer_OnStudyCreated> _logger;

    public SearchIndexer_OnStudyCreated(
        ISearchIndexRepository index,
        ISearchUnitOfWork unitOfWork,
        IStudyRepository studyRepository,
        ILogger<SearchIndexer_OnStudyCreated> logger)
    {
        _index = index;
        _unitOfWork = unitOfWork;
        _studyRepository = studyRepository;
        _logger = logger;
    }

    public async Task Handle(StudyCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var study = await _studyRepository.GetByIdAsync(notification.StudyId, cancellationToken);
            if (study is null) return;

            var objectId = study.Id.ToString();
            var title = study.Title.ToString();
            var body = study.Description.ToString();
            var tags = study.Tags is { Count: > 0 } t ? string.Join(",", t) : null;
            var ownerId = study.OwnerId.ToString();
            var isPublic = study.IsPublic;

            var existing = await _index.GetAsync(SearchObjectType.Study, objectId, cancellationToken);
            if (existing is not null)
            {
                existing.Update(title, body, tags, isPublic, ownerId);
                _index.Update(existing);
            }
            else
            {
                var entry = SearchIndexEntry.Create(
                    SearchObjectType.Study, objectId, ownerId, title, body, tags, isPublic);
                if (entry.IsFailure) return;
                await _index.AddAsync(entry.Value, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SearchIndexer failed for StudyCreated {StudyId}", notification.StudyId);
        }
    }
}
