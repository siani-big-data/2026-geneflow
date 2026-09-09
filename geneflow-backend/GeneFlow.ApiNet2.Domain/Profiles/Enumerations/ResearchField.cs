using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Profiles.Enumerations;

/// <summary>
/// Research fields for scientific profiles.
/// </summary>
public sealed class ResearchField : Enumeration<ResearchField>
{
    /// <summary>Genomics research field.</summary>
    public static readonly ResearchField Genomics = new(1, nameof(Genomics));

    /// <summary>Proteomics research field.</summary>
    public static readonly ResearchField Proteomics = new(2, nameof(Proteomics));

    /// <summary>Transcriptomics research field.</summary>
    public static readonly ResearchField Transcriptomics = new(3, nameof(Transcriptomics));

    /// <summary>Bioinformatics research field.</summary>
    public static readonly ResearchField Bioinformatics = new(4, nameof(Bioinformatics));

    /// <summary>Molecular Biology research field.</summary>
    public static readonly ResearchField MolecularBiology = new(5, nameof(MolecularBiology));

    /// <summary>Cell Biology research field.</summary>
    public static readonly ResearchField CellBiology = new(6, nameof(CellBiology));

    /// <summary>Genetics research field.</summary>
    public static readonly ResearchField Genetics = new(7, nameof(Genetics));

    /// <summary>Biochemistry research field.</summary>
    public static readonly ResearchField Biochemistry = new(8, nameof(Biochemistry));

    /// <summary>Microbiology research field.</summary>
    public static readonly ResearchField Microbiology = new(9, nameof(Microbiology));

    /// <summary>Immunology research field.</summary>
    public static readonly ResearchField Immunology = new(10, nameof(Immunology));

    /// <summary>Neuroscience research field.</summary>
    public static readonly ResearchField Neuroscience = new(11, nameof(Neuroscience));

    /// <summary>Plant Biology research field.</summary>
    public static readonly ResearchField PlantBiology = new(12, nameof(PlantBiology));

    /// <summary>Marine Biology research field.</summary>
    public static readonly ResearchField MarineBiology = new(13, nameof(MarineBiology));

    /// <summary>Ecology research field.</summary>
    public static readonly ResearchField Ecology = new(14, nameof(Ecology));

    /// <summary>Evolutionary Biology research field.</summary>
    public static readonly ResearchField EvolutionaryBiology = new(15, nameof(EvolutionaryBiology));

    /// <summary>Computational Biology research field.</summary>
    public static readonly ResearchField ComputationalBiology = new(16, nameof(ComputationalBiology));

    /// <summary>Synthetic Biology research field.</summary>
    public static readonly ResearchField SyntheticBiology = new(17, nameof(SyntheticBiology));

    /// <summary>Other research field.</summary>
    public static readonly ResearchField Other = new(18, nameof(Other));

    private ResearchField(int id, string name) : base(id, name) { }
}
