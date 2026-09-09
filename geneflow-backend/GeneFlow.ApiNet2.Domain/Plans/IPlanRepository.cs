namespace GeneFlow.ApiNet2.Domain.Plans;

/// <summary>
/// Repository interface for Plan aggregate.
/// </summary>
public interface IPlanRepository
{
    /// <summary>
    /// Gets a plan by its ID.
    /// </summary>
    Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active plans ordered by display order.
    /// </summary>
    Task<IReadOnlyList<Plan>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default plan.
    /// </summary>
    Task<Plan?> GetDefaultPlanAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a plan with the given name exists.
    /// </summary>
    Task<bool> ExistsWithNameAsync(string name, PlanId? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new plan.
    /// </summary>
    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing plan.
    /// </summary>
    void Update(Plan plan);
}
