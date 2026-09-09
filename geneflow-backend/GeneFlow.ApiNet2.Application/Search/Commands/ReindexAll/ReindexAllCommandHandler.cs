using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Search;
using GeneFlow.ApiNet2.Domain.Search.Entities;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Application.Search.Commands.ReindexAll;

/// <summary>
/// Streams every non-deleted Study and Discussion into the search index in
/// a single transaction. Upsert semantics: existing entries are updated to
/// reflect the current state of the source aggregate.
/// </summary>
public sealed class ReindexAllCommandHandler
    : ICommandHandler<ReindexAllCommand, Result<ReindexAllResult>>
{
    private readonly ISearchIndexRepository _index;
    private readonly ISearchUnitOfWork _unitOfWork;
    private readonly IStudyRepository _studyRepository;
    private readonly IDiscussionRepository _discussionRepository;
    private readonly ILogger<ReindexAllCommandHandler> _logger;

    public ReindexAllCommandHandler(
        ISearchIndexRepository index,
        ISearchUnitOfWork unitOfWork,
        IStudyRepository studyRepository,
        IDiscussionRepository discussionRepository,
        ILogger<ReindexAllCommandHandler> logger)
    {
        _index = index;
        _unitOfWork = unitOfWork;
        _studyRepository = studyRepository;
        _discussionRepository = discussionRepository;
        _logger = logger;
    }

    public async Task<Result<ReindexAllResult>> Handle(
        ReindexAllCommand request, CancellationToken cancellationToken)
    {
        var studies = await _studyRepository.ListAllForReindexAsync(cancellationToken);
        var discussions = await _discussionRepository.ListAllForReindexAsync(cancellationToken);

        // Build a lookup so we don't query IsPublicStudyAsync per discussion.
        var publicByStudy = studies.ToDictionary(s => s.Id.ToString(), s => s.IsPublic);

        var studiesIndexed = 0;
        foreach (var study in studies)
        {
            var objectId = study.Id.ToString();
            var title = study.Title.ToString();
            var body = study.Description.ToString();
            var tags = study.Tags is { Count: > 0 } t ? string.Join(",", t) : null;
            var ownerId = study.OwnerId.ToString();

            var existing = await _index.GetAsync(
                SearchObjectType.Study, objectId, cancellationToken);
            if (existing is not null)
            {
                existing.Update(title, body, tags, study.IsPublic, ownerId);
                _index.Update(existing);
            }
            else
            {
                var entry = SearchIndexEntry.Create(
                    SearchObjectType.Study, objectId, ownerId, title, body, tags, study.IsPublic);
                if (entry.IsFailure)
                    continue;
                await _index.AddAsync(entry.Value, cancellationToken);
            }
            studiesIndexed++;
        }

        var discussionsIndexed = 0;
        foreach (var discussion in discussions)
        {
            var objectId = discussion.Id.ToString();
            var ownerId = discussion.AuthorId.ToString();
            // tags carries the parent study id (see SearchIndexer_OnDiscussionCreated).
            var tags = discussion.StudyId.ToString();
            var isPublic = publicByStudy.TryGetValue(discussion.StudyId.ToString(), out var p) && p;

            var existing = await _index.GetAsync(
                SearchObjectType.Discussion, objectId, cancellationToken);
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
                if (entry.IsFailure)
                    continue;
                await _index.AddAsync(entry.Value, cancellationToken);
            }
            discussionsIndexed++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reindex completed. Studies: {Studies}, Discussions: {Discussions}",
            studiesIndexed, discussionsIndexed);

        return Result<ReindexAllResult>.Success(
            new ReindexAllResult(studiesIndexed, discussionsIndexed));
    }
}
