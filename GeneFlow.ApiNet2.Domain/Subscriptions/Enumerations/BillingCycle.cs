using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Subscriptions.Enumerations;

/// <summary>
/// Enumeration for billing cycle.
/// </summary>
public sealed class BillingCycle : Enumeration<BillingCycle>
{
    /// <summary>Monthly billing cycle.</summary>
    public static readonly BillingCycle Monthly = new(1, nameof(Monthly), 1);

    /// <summary>Annual billing cycle.</summary>
    public static readonly BillingCycle Annual = new(2, nameof(Annual), 12);

    /// <summary>Gets the number of months in this billing cycle.</summary>
    public int Months { get; }

    private BillingCycle(int id, string name, int months) : base(id, name)
    {
        Months = months;
    }

    /// <summary>
    /// Calculates the next billing date from a given start date.
    /// </summary>
    public DateTime CalculateNextBillingDate(DateTime startDate)
    {
        return startDate.AddMonths(Months);
    }
}
