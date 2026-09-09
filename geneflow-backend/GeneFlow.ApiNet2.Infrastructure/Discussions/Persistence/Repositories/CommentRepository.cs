using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private readonly DiscussionContext _context;

    public CommentRepository(DiscussionContext context)
    {
        _context = context;
    }

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Comment>> GetForParentAsync(
        CommentParentType parentType,
        string parentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Where(c => c.ParentType == parentType && c.ParentId == parentId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountForParentAsync(
        CommentParentType parentType,
        string parentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Comments
            .Where(c => c.ParentType == parentType && c.ParentId == parentId && !c.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _context.Comments.AddAsync(comment, cancellationToken);
    }

    public void Update(Comment comment)
    {
        _context.Comments.Update(comment);
    }
}
