using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;

namespace GeneFlow.ApiNet2.Domain.Discussions;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all (non-deleted) comments for a parent in chronological order.
    /// Soft-deleted comments are excluded; callers needing the full history
    /// (admin tooling) should query the DbContext directly.
    /// </summary>
    Task<IReadOnlyList<Comment>> GetForParentAsync(
        CommentParentType parentType,
        string parentId,
        CancellationToken cancellationToken = default);

    Task<int> CountForParentAsync(
        CommentParentType parentType,
        string parentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);

    void Update(Comment comment);
}
