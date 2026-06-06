using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Identity;

namespace GeneFlow.ApiNet2.Domain.Discussions;

public interface IReactionRepository
{
    Task<Reaction?> GetAsync(Guid commentId, UserId userId, string emoji, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reaction>> GetForCommentsAsync(IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken = default);

    Task AddAsync(Reaction reaction, CancellationToken cancellationToken = default);

    void Remove(Reaction reaction);
}
