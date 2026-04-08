using GeneFlow.ApiNet2.Domain.Plans;
using GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Plans.Persistence.Repositories;

/// <summary>
/// Repository implementation for Plan aggregate.
/// </summary>
public sealed class PlanRepository : IPlanRepository
{
    private readonly PlanContext _context;

    /// <summary>
    /// Initializes a new instance of the repository.
    /// </summary>
    public PlanRepository(PlanContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Plan?> GetDefaultPlanAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .FirstOrDefaultAsync(p => p.IsDefault && p.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithNameAsync(string name, PlanId? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Plans.Where(p => p.Name.Value == name);

        if (excludeId is not null)
            query = query.Where(p => p.Id != excludeId);

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        await _context.Plans.AddAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Plan plan)
    {
        _context.Plans.Update(plan);
    }
}
