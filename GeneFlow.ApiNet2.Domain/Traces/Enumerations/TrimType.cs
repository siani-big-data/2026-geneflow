using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Traces.Enumerations;

/// <summary>
/// Types of trim operations that can be applied to a trace sequence.
/// </summary>
public sealed class TrimType : Enumeration<TrimType>
{
    public static readonly TrimType Manual = new(1, nameof(Manual), "Manual Trim");
    public static readonly TrimType AutoMott = new(2, nameof(AutoMott), "Mott Algorithm");
    public static readonly TrimType AutoWindow = new(3, nameof(AutoWindow), "Sliding Window");

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    private TrimType(int id, string name, string displayName) : base(id, name)
    {
        DisplayName = displayName;
    }

    /// <summary>
    /// Whether this trim type is automatic (algorithm-based).
    /// </summary>
    public bool IsAutomatic => this == AutoMott || this == AutoWindow;
}
