using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Search.Enumerations;

/// <summary>
/// Discriminator for the heterogenous corpus that the search index
/// stores. Tied to the bounded context the indexed object lives in.
/// </summary>
public sealed class SearchObjectType : Enumeration<SearchObjectType>
{
    /// <summary>A research study.</summary>
    public static readonly SearchObjectType Study = new(1, nameof(Study));

    /// <summary>A sequencing trace.</summary>
    public static readonly SearchObjectType Trace = new(2, nameof(Trace));

    /// <summary>A discussion thread (title + first comment body).</summary>
    public static readonly SearchObjectType Discussion = new(3, nameof(Discussion));

    /// <summary>A user public profile.</summary>
    public static readonly SearchObjectType User = new(4, nameof(User));

    private SearchObjectType(int id, string name) : base(id, name) { }
}
