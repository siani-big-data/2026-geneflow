using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

/// <summary>
/// Represents study engagement metrics (views, stars).
/// </summary>
public sealed class StudyMetrics : ValueObject
{
    public int ViewsCount { get; }
    public int StarsCount { get; }

    private StudyMetrics(int viewsCount, int starsCount)
    {
        ViewsCount = viewsCount;
        StarsCount = starsCount;
    }

    public static StudyMetrics Empty => new(0, 0);

    public static StudyMetrics Create(int viewsCount, int starsCount)
        => new(Math.Max(0, viewsCount), Math.Max(0, starsCount));

    public StudyMetrics IncrementViews() => new(ViewsCount + 1, StarsCount);
    public StudyMetrics IncrementStars() => new(ViewsCount, StarsCount + 1);
    public StudyMetrics DecrementStars() => new(ViewsCount, Math.Max(0, StarsCount - 1));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ViewsCount;
        yield return StarsCount;
    }
}
