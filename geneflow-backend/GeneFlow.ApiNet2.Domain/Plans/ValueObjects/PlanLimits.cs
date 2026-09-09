using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

/// <summary>
/// Value object representing plan limits.
/// </summary>
public sealed class PlanLimits : ValueObject
{
    /// <summary>Value representing unlimited.</summary>
    public const int Unlimited = -1;

    /// <summary>Gets the maximum number of studies. -1 = Unlimited.</summary>
    public int MaxStudies { get; }

    /// <summary>Gets the maximum traces per month. -1 = Unlimited.</summary>
    public int MaxTracesPerMonth { get; }

    /// <summary>Gets the maximum members per study. -1 = Unlimited.</summary>
    public int MaxMembersPerStudy { get; }

    /// <summary>Gets whether studies are unlimited.</summary>
    public bool IsUnlimitedStudies => MaxStudies == Unlimited;

    /// <summary>Gets whether traces are unlimited.</summary>
    public bool IsUnlimitedTraces => MaxTracesPerMonth == Unlimited;

    /// <summary>Gets whether members are unlimited.</summary>
    public bool IsUnlimitedMembers => MaxMembersPerStudy == Unlimited;

    private PlanLimits(int maxStudies, int maxTracesPerMonth, int maxMembersPerStudy)
    {
        MaxStudies = maxStudies;
        MaxTracesPerMonth = maxTracesPerMonth;
        MaxMembersPerStudy = maxMembersPerStudy;
    }

    /// <summary>
    /// Creates validated PlanLimits.
    /// </summary>
    public static Result<PlanLimits> Create(int maxStudies, int maxTracesPerMonth, int maxMembersPerStudy)
    {
        if (maxStudies < Unlimited || maxStudies == 0)
            return Result.Failure<PlanLimits>(PlanErrors.InvalidMaxStudies);

        if (maxTracesPerMonth < Unlimited || maxTracesPerMonth == 0)
            return Result.Failure<PlanLimits>(PlanErrors.InvalidMaxTraces);

        if (maxMembersPerStudy < Unlimited || maxMembersPerStudy == 0)
            return Result.Failure<PlanLimits>(PlanErrors.InvalidMaxMembers);

        return new PlanLimits(maxStudies, maxTracesPerMonth, maxMembersPerStudy);
    }

    /// <summary>
    /// Creates unlimited limits.
    /// </summary>
    public static PlanLimits CreateUnlimited() => new(Unlimited, Unlimited, Unlimited);

    /// <summary>
    /// Creates free tier limits.
    /// </summary>
    public static PlanLimits FreeTier() => new(2, 50, 3);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxStudies;
        yield return MaxTracesPerMonth;
        yield return MaxMembersPerStudy;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"Studies: {(IsUnlimitedStudies ? "∞" : MaxStudies)}, " +
        $"Traces/mo: {(IsUnlimitedTraces ? "∞" : MaxTracesPerMonth)}, " +
        $"Members: {(IsUnlimitedMembers ? "∞" : MaxMembersPerStudy)}";
}
