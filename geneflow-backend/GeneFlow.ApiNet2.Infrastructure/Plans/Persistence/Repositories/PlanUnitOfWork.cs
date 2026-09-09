using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Context;

namespace GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Repositories;

/// <summary>
/// Unit of work implementation for Plan bounded context.
/// </summary>
public sealed class PlanUnitOfWork : IPlanUnitOfWork
{
    private readonly PlanContext _context;

    /// <summary>
    /// Initializes a new instance of the unit of work.
    /// </summary>
    public PlanUnitOfWork(PlanContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
