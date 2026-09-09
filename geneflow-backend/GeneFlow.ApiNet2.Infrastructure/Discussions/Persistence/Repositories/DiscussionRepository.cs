using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Repositories;

public sealed class DiscussionRepository : IDiscussionRepository
{
    private readonly DiscussionContext _context;
    private readonly ISequenceGenerator _sequenceGenerator;

    public DiscussionRepository(DiscussionContext context, ISequenceGenerator sequenceGenerator)
    {
        _context = context;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<Discussion?> GetByIdAsync(DiscussionId id, CancellationToken cancellationToken = default)
    {
        return await _context.Discussions
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<PagedList<Discussion>> GetByStudyAsync(
        StudyId studyId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Discussions.Where(d => d.StudyId == studyId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(d => EF.Functions.ILike(d.Title, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(d => d.Category == category);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<Discussion>.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task AddAsync(Discussion discussion, CancellationToken cancellationToken = default)
    {
        await _context.Discussions.AddAsync(discussion, cancellationToken);
    }

    public void Update(Discussion discussion)
    {
        _context.Discussions.Update(discussion);
    }

    public Task<long> GetNextSequenceValueAsync(CancellationToken cancellationToken = default)
    {
        return _sequenceGenerator.NextAsync(DiscussionId.SequenceName, cancellationToken);
    }

    public async Task<IReadOnlyList<Discussion>> ListAllForReindexAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Discussions
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}
