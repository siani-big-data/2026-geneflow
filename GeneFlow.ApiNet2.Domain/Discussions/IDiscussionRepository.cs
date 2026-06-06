using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Discussions;

public interface IDiscussionRepository
{
    Task<Discussion?> GetByIdAsync(DiscussionId id, CancellationToken cancellationToken = default);

    Task<PagedList<Discussion>> GetByStudyAsync(
        StudyId studyId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? category = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(Discussion discussion, CancellationToken cancellationToken = default);

    void Update(Discussion discussion);

    Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default);
}
