using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Traces.Enumerations;

/// <summary>
/// DNA strand orientation for annotations.
/// </summary>
public sealed class AnnotationStrand : Enumeration<AnnotationStrand>
{
    public static readonly AnnotationStrand Plus = new(1, nameof(Plus), "+", "Forward strand (5' to 3')");
    public static readonly AnnotationStrand Minus = new(2, nameof(Minus), "-", "Reverse strand (3' to 5')");
    public static readonly AnnotationStrand None = new(3, nameof(None), ".", "No strand orientation");

    /// <summary>
    /// Symbol representation (+, -, or .)
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; }

    private AnnotationStrand(int id, string name, string symbol, string description)
        : base(id, name)
    {
        Symbol = symbol;
        Description = description;
    }

    /// <summary>
    /// Gets the strand from its symbol representation.
    /// </summary>
    public static AnnotationStrand? FromSymbol(string symbol)
    {
        return symbol switch
        {
            "+" => Plus,
            "-" => Minus,
            "." or "" => None,
            _ => null
        };
    }
}
