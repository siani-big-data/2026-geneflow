using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Plans.ValueObjects;

/// <summary>
/// Value object representing plan pricing.
/// </summary>
public sealed class PlanPricing : ValueObject
{
    /// <summary>Gets the monthly price.</summary>
    public decimal MonthlyPrice { get; }

    /// <summary>Gets the annual price.</summary>
    public decimal AnnualPrice { get; }

    /// <summary>Gets the currency code (ISO 4217).</summary>
    public string Currency { get; }

    /// <summary>Gets whether this is a free plan.</summary>
    public bool IsFree => MonthlyPrice == 0 && AnnualPrice == 0;

    /// <summary>Gets the monthly equivalent from annual price.</summary>
    public decimal MonthlyEquivalentFromAnnual => AnnualPrice / 12;

    /// <summary>Gets the annual discount percentage.</summary>
    public decimal AnnualDiscountPercentage
    {
        get
        {
            if (MonthlyPrice == 0)
                return 0;
            var fullYearPrice = MonthlyPrice * 12;
            return Math.Round((fullYearPrice - AnnualPrice) / fullYearPrice * 100, 2);
        }
    }

    private PlanPricing(decimal monthlyPrice, decimal annualPrice, string currency)
    {
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        Currency = currency;
    }

    /// <summary>
    /// Creates a validated PlanPricing.
    /// </summary>
    public static Result<PlanPricing> Create(decimal monthlyPrice, decimal annualPrice, string currency = "EUR")
    {
        if (monthlyPrice < 0)
            return Result.Failure<PlanPricing>(PlanErrors.InvalidMonthlyPrice);

        if (annualPrice < 0)
            return Result.Failure<PlanPricing>(PlanErrors.InvalidAnnualPrice);

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Failure<PlanPricing>(PlanErrors.InvalidCurrency);

        return new PlanPricing(monthlyPrice, annualPrice, currency.ToUpperInvariant());
    }

    /// <summary>
    /// Creates a free pricing.
    /// </summary>
    public static PlanPricing Free() => new(0, 0, "EUR");

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MonthlyPrice;
        yield return AnnualPrice;
        yield return Currency;
    }

    /// <inheritdoc />
    public override string ToString() => IsFree ? "Free" : $"{MonthlyPrice} {Currency}/month";
}
