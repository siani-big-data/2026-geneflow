using GeneFlow.ApiNet2.Domain.Plans.Enumerations;
using GeneFlow.ApiNet2.Domain.Plans.Events;
using GeneFlow.ApiNet2.Domain.Plans.ValueObjects;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Plans;

/// <summary>
/// Plan aggregate root representing a subscription plan.
/// </summary>
public sealed class Plan : AuditableAggregateRoot<PlanId>
{
    private readonly List<PlanFeature> _features = new();

    /// <summary>Gets the plan name.</summary>
    public PlanName Name { get; private set; } = null!;

    /// <summary>Gets the plan description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the plan pricing.</summary>
    public PlanPricing Pricing { get; private set; } = null!;

    /// <summary>Gets the plan limits.</summary>
    public PlanLimits Limits { get; private set; } = null!;

    /// <summary>Gets whether the plan is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets whether this is the default plan.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>Gets the display order.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Gets the plan features.</summary>
    public IReadOnlyList<PlanFeature> Features => _features.AsReadOnly();

    /// <summary>Gets whether this is a free plan.</summary>
    public bool IsFree => Pricing.IsFree;

    private Plan()
    {
    }

    /// <summary>
    /// Creates a new Plan.
    /// </summary>
    public static Result<Plan> Create(
        PlanName name,
        string? description,
        PlanPricing pricing,
        PlanLimits limits,
        int displayOrder = 0,
        bool isDefault = false)
    {
        var plan = new Plan
        {
            Id = PlanId.New(),
            Name = name,
            Description = description,
            Pricing = pricing,
            Limits = limits,
            IsActive = true,
            IsDefault = isDefault,
            DisplayOrder = displayOrder
        };

        plan.RaiseDomainEvent(new PlanCreatedEvent(plan.Id));

        return plan;
    }

    /// <summary>
    /// Updates plan details.
    /// </summary>
    public Result UpdateDetails(PlanName name, string? description, int displayOrder)
    {
        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
        return Result.Success();
    }

    /// <summary>
    /// Updates plan pricing.
    /// </summary>
    public Result UpdatePricing(PlanPricing pricing)
    {
        Pricing = pricing;
        return Result.Success();
    }

    /// <summary>
    /// Updates plan limits.
    /// </summary>
    public Result UpdateLimits(PlanLimits limits)
    {
        Limits = limits;
        return Result.Success();
    }

    /// <summary>
    /// Activates the plan.
    /// </summary>
    public Result Activate()
    {
        IsActive = true;
        return Result.Success();
    }

    /// <summary>
    /// Deactivates the plan.
    /// </summary>
    public Result Deactivate()
    {
        if (IsDefault)
            return Result.Failure(PlanErrors.CannotDeactivateDefaultPlan);

        IsActive = false;
        return Result.Success();
    }

    /// <summary>
    /// Sets this plan as the default.
    /// </summary>
    public Result SetAsDefault()
    {
        IsDefault = true;
        return Result.Success();
    }

    /// <summary>
    /// Removes the default status.
    /// </summary>
    public Result RemoveDefaultStatus()
    {
        IsDefault = false;
        return Result.Success();
    }

    /// <summary>
    /// Adds a feature to the plan.
    /// </summary>
    public Result AddFeature(PlanFeature feature)
    {
        if (_features.Contains(feature))
            return Result.Failure(PlanErrors.FeatureAlreadyExists);

        _features.Add(feature);
        return Result.Success();
    }

    /// <summary>
    /// Removes a feature from the plan.
    /// </summary>
    public Result RemoveFeature(PlanFeature feature)
    {
        if (!_features.Contains(feature))
            return Result.Failure(PlanErrors.FeatureNotFound);

        _features.Remove(feature);
        return Result.Success();
    }

    /// <summary>
    /// Checks if the plan has a specific feature.
    /// </summary>
    public bool HasFeature(PlanFeature feature) => _features.Contains(feature);
}
