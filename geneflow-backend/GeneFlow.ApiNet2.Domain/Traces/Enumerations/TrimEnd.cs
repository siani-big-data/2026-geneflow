using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Traces.Enumerations;

/// <summary>
/// Indicates which end of the sequence is being trimmed.
/// </summary>
public sealed class TrimEnd : Enumeration<TrimEnd>
{
    public static readonly TrimEnd FivePrime = new(1, nameof(FivePrime), "5' End");
    public static readonly TrimEnd ThreePrime = new(2, nameof(ThreePrime), "3' End");

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    private TrimEnd(int id, string name, string displayName) : base(id, name)
    {
        DisplayName = displayName;
    }
}
