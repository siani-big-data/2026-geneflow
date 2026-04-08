using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Subscriptions.ValueObjects;

/// <summary>
/// Value object representing a subscription period.
/// </summary>
public sealed class SubscriptionPeriod : ValueObject
{
    /// <summary>Gets the period start date.</summary>
    public DateTime StartDate { get; }

    /// <summary>Gets the period end date.</summary>
    public DateTime EndDate { get; }

    /// <summary>Gets whether the period is currently active.</summary>
    public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow < EndDate;

    /// <summary>Gets whether the period has expired.</summary>
    public bool HasExpired => DateTime.UtcNow >= EndDate;

    /// <summary>Gets the number of days remaining.</summary>
    public int DaysRemaining => HasExpired ? 0 : (int)(EndDate - DateTime.UtcNow).TotalDays;

    /// <summary>Gets the duration of the period.</summary>
    public TimeSpan Duration => EndDate - StartDate;

    private SubscriptionPeriod(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// Creates a validated SubscriptionPeriod.
    /// </summary>
    public static Result<SubscriptionPeriod> Create(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            return Result.Failure<SubscriptionPeriod>(SubscriptionErrors.InvalidPeriod);

        return new SubscriptionPeriod(startDate, endDate);
    }

    /// <summary>
    /// Creates a period starting from now.
    /// </summary>
    public static SubscriptionPeriod CreateFromNow(int months)
    {
        var start = DateTime.UtcNow;
        var end = start.AddMonths(months);
        return new SubscriptionPeriod(start, end);
    }

    /// <summary>
    /// Creates a period starting from a specific date.
    /// </summary>
    public static SubscriptionPeriod CreateFromDate(DateTime startDate, int months)
    {
        var end = startDate.AddMonths(months);
        return new SubscriptionPeriod(startDate, end);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    /// <inheritdoc />
    public override string ToString() => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
}
