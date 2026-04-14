using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Traces.Enumerations;

/// <summary>
/// Types of sequence edits that can be made to a trace.
/// </summary>
public sealed class EditType : Enumeration<EditType>
{
    public static readonly EditType Change = new(1, nameof(Change), "Base Change");
    public static readonly EditType Insert = new(2, nameof(Insert), "Base Insertion");
    public static readonly EditType Delete = new(3, nameof(Delete), "Base Deletion");

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    private EditType(int id, string name, string displayName) : base(id, name)
    {
        DisplayName = displayName;
    }

    /// <summary>
    /// Whether this edit type requires an original base.
    /// </summary>
    public bool RequiresOriginalBase => this == Change || this == Delete;

    /// <summary>
    /// Whether this edit type requires a new base.
    /// </summary>
    public bool RequiresNewBase => this == Change || this == Insert;
}
