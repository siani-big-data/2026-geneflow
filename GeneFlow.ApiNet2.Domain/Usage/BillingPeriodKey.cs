namespace GeneFlow.ApiNet2.Domain.Usage;

/// <summary>
/// Represents a billing period key in format "YYYY-MM".
/// Used as a Redis key component for period-specific statistics.
/// </summary>
public sealed record BillingPeriodKey
{
    /// <summary>
    /// The year of the billing period.
    /// </summary>
    public int Year { get; }

    /// <summary>
    /// The month of the billing period (1-12).
    /// </summary>
    public int Month { get; }

    private BillingPeriodKey(int year, int month)
    {
        Year = year;
        Month = month;
    }

    /// <summary>
    /// Creates a billing period key from a date.
    /// </summary>
    public static BillingPeriodKey FromDate(DateTime date)
    {
        return new BillingPeriodKey(date.Year, date.Month);
    }

    /// <summary>
    /// Creates the current billing period key.
    /// </summary>
    public static BillingPeriodKey Current()
    {
        return FromDate(DateTime.UtcNow);
    }

    /// <summary>
    /// Parses a billing period key from string format "YYYY-MM".
    /// </summary>
    public static BillingPeriodKey Parse(string value)
    {
        var parts = value.Split('-');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid billing period key format: {value}");

        return new BillingPeriodKey(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    /// <summary>
    /// Tries to parse a billing period key from string.
    /// </summary>
    public static bool TryParse(string? value, out BillingPeriodKey? result)
    {
        result = null;
        if (string.IsNullOrEmpty(value))
            return false;

        var parts = value.Split('-');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
            return false;

        if (month < 1 || month > 12)
            return false;

        result = new BillingPeriodKey(year, month);
        return true;
    }

    /// <summary>
    /// Returns the string representation "YYYY-MM".
    /// </summary>
    public override string ToString() => $"{Year:D4}-{Month:D2}";

    /// <summary>
    /// Gets the next billing period.
    /// </summary>
    public BillingPeriodKey Next()
    {
        if (Month == 12)
            return new BillingPeriodKey(Year + 1, 1);
        return new BillingPeriodKey(Year, Month + 1);
    }

    /// <summary>
    /// Gets the previous billing period.
    /// </summary>
    public BillingPeriodKey Previous()
    {
        if (Month == 1)
            return new BillingPeriodKey(Year - 1, 12);
        return new BillingPeriodKey(Year, Month - 1);
    }

    /// <summary>
    /// Checks if this period is the current period.
    /// </summary>
    public bool IsCurrent()
    {
        var now = DateTime.UtcNow;
        return Year == now.Year && Month == now.Month;
    }
}
