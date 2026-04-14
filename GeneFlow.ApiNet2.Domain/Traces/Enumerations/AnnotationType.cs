using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Traces.Enumerations;

/// <summary>
/// Types of annotations that can be added to a trace sequence.
/// </summary>
public sealed class AnnotationType : Enumeration<AnnotationType>
{
    public static readonly AnnotationType Region = new(1, nameof(Region), "Region", true);
    public static readonly AnnotationType Point = new(2, nameof(Point), "Point", false);
    public static readonly AnnotationType Feature = new(3, nameof(Feature), "Feature", true);
    public static readonly AnnotationType Custom = new(4, nameof(Custom), "Custom", true);

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Whether this annotation type covers a range (start to end).
    /// </summary>
    public bool IsRange { get; }

    private AnnotationType(int id, string name, string displayName, bool isRange)
        : base(id, name)
    {
        DisplayName = displayName;
        IsRange = isRange;
    }
}
