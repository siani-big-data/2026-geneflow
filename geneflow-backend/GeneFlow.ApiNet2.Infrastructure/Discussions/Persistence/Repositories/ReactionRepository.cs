using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Repositories;

public sealed class ReactionRepository : IReactionRepository
{
    private readonly DiscussionContext _context;

    public ReactionRepository(DiscussionContext context)
    {
        _context = context;
    }

    public async Task<Reaction?> GetAsync(
        Guid commentId,
        UserId userId,
        string emoji,
        CancellationToken cancellationToken = default)
    {
        return await _context.Reactions
            .FirstOrDefaultAsync(
                r => r.CommentId == commentId && r.UserId == userId && r.Emoji == emoji,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Reaction>> GetForCommentsAsync(
        IReadOnlyCollection<Guid> commentIds,
        CancellationToken cancellationToken = default)
    {
        if (commentIds.Count == 0)
            return Array.Empty<Reaction>();

        return await _context.Reactions
            .Where(r => commentIds.Contains(r.CommentId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Reaction reaction, CancellationToken cancellationToken = default)
    {
        await _context.Reactions.AddAsync(reaction, cancellationToken);
    }

    public void Remove(Reaction reaction)
    {
        _context.Reactions.Remove(reaction);
    }
}
